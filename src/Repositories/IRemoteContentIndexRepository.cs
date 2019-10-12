using EPiServer.Core;
using EPiServer.Events.Clients;
using EPiServer.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using EPiServer.ServiceLocation;
using System.Configuration;
using System.IO;
using EPiServer.Data.Dynamic;
using EPiServer.DynamicLuceneExtensions.Models;
using EPiServer.DynamicLuceneExtensions.Services;
using EPiServer.DynamicLuceneExtensions.Configurations;
using Lucene.Net.Store;

namespace EPiServer.DynamicLuceneExtensions.Repositories
{
    public interface IRemoteContentIndexRepository
    {
        void Init();

        void IndexContent(IContent content, bool includeChild = false);
        void RemoveContentIndex(IContent content, bool includeChild = false);
        void ReindexSite(IContent content);

        void ResetIndexDirectory(Guid? targetMachine = null);

        long GetIndexFolderSize();

        void RecoverIndex(Guid? targetMachine = null);
    }

    [ServiceConfiguration(typeof(IRemoteContentIndexRepository), Lifecycle = ServiceInstanceScope.Singleton)]
    public class RemoteContentIndexRepository : IRemoteContentIndexRepository
    {
        public static readonly Guid LocalRaiserId = Guid.NewGuid();
        public static readonly Guid IndexContentEventId = new Guid("{e5a66653-83fe-4de1-b69b-26c5134685ea}");
        private readonly IIndexRecoveryService _indexRecoveryService;
        private readonly IEventRegistry _eventService;
        private readonly IContentIndexRepository _contentIndexRepository;
        public RemoteContentIndexRepository(IContentIndexRepository contentIndexRepository, IEventRegistry eventService
            , IIndexRecoveryService indexRecoveryService)
        {
            _contentIndexRepository = contentIndexRepository;
            _eventService = eventService;
            _indexRecoveryService = indexRecoveryService;
        }
        public void IndexContent(IContent content, bool includeChild = false)
        {
            _contentIndexRepository.IndexContent(content, includeChild);
            if (ShouldRaiseEvent)
            {
                var indexRequest = new IndexRequestItem(content, IndexRequestItem.REINDEX, includeChild);
                var param = indexRequest.RemoteRequest;
                this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
            }
        }

        public void RemoveContentIndex(IContent content, bool includeChild = false)
        {
            _contentIndexRepository.RemoveContentIndex(content, includeChild);
            if (ShouldRaiseEvent)
            {
                var indexRequest = new IndexRequestItem(content, IndexRequestItem.REMOVE, includeChild);
                var param = indexRequest.RemoteRequest;
                this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
            }
        }


        public void ReindexSite(IContent content)
        {
            _contentIndexRepository.ReindexSite(content);
            if (ShouldRaiseEvent)
            {
                var indexRequest = new IndexRequestItem(content, IndexRequestItem.REINDEXSITE, true);
                var param = indexRequest.RemoteRequest;
                this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
            }
        }
        public virtual void ResetIndexDirectory(Guid? targetMachine = null)
        {
            if (targetMachine == null || targetMachine == LocalRaiserId)
            {
                _contentIndexRepository.ResetIndexDirectory();
            }
            else if (ShouldRaiseEvent)
            {
                var param = new ResetIndexRequestItem(targetMachine).RemoteRequest;
                this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
            }
        }

        public void RecoverIndex(Guid? targetMachine = null)
        {
            if (targetMachine == null || targetMachine == LocalRaiserId)
            {
                _indexRecoveryService.RecoverIndex(true);
            }
            else if (ShouldRaiseEvent)
            {
                var param = new RecoverIndexRequestItem(targetMachine).RemoteRequest;
                this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
            }
        }

        private bool ShouldRaiseEvent
        {
            get
            {
                return LuceneContext.ShouldRaiseRemoteEvent;
            }
        }

        public virtual long GetIndexFolderSize()
        {
            var folderSize = _contentIndexRepository.GetIndexFolderSize();
            var param = new GetIndexSizeRequestItem().RemoteRequest;
            //var store = new SqlDataStore<ServerInfomation>();
            //store.DeleteAll();
            //store.Save(new ServerInfomation()
            //{
            //    IndexSize = folderSize,
            //    LocalRaiserId = LocalRaiserId,
            //    Name = Environment.MachineName,
            //    InRecovering = IndexRecoveryService.IN_RECOVERING,
            //    InHealthChecking = IndexHealthCheckService.IS_HEALTH_CHECK
            //});
            if (ShouldRaiseEvent)
                this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
            return folderSize;
        }

        private void IndexContent_Raised(object sender, EventNotificationEventArgs e)
        {
            if (e.RaiserId == LocalRaiserId)
                return;
            this.ProcessRequest((string)e.Param);
        }
        public virtual void ProcessRequest(string indexRequest)
        {
            if (string.IsNullOrEmpty(indexRequest)) return;
            var request = IndexRequestItem.Parse(indexRequest);
            if (request == null) return;
            if (string.IsNullOrEmpty(request.Action)) return;
            switch (request.Action)
            {
                case IndexRequestItem.REINDEX:
                    if (request.Content != null)
                        _contentIndexRepository.IndexContent(request.Content, request.IncludeChild);
                    break;
                case IndexRequestItem.REMOVE:
                    if (request.Content != null)
                        _contentIndexRepository.RemoveContentIndex(request.Content, request.IncludeChild);
                    break;
                case IndexRequestItem.REMOVE_LANGUAGE:
                    if (request.Content != null)
                        _contentIndexRepository.RemoveContentLanguageBranch(request.Content);
                    break;
                case IndexRequestItem.REINDEXSITE:
                    if (request.Content != null)
                        _contentIndexRepository.ReindexSite(request.Content);
                    break;
                case IndexRequestItem.CALCULATESIZE:
                    var folderSize = _contentIndexRepository.GetIndexFolderSize();
                    var store = typeof(ServerInfomation).GetOrCreateStore();
                    store.Save(new ServerInfomation()
                    {
                        IndexSize = folderSize,
                        LocalRaiserId = LocalRaiserId,
                        Name = Environment.MachineName,
                        InRecovering = IndexRecoveryService.IN_RECOVERING,
                        InHealthChecking = IndexHealthCheckService.IS_HEALTH_CHECK
                    });
                    break;
                case IndexRequestItem.RESETINDEX:
                    var resetRequest = (ResetIndexRequestItem)request;
                    if (resetRequest.TargetMachine != null && resetRequest.TargetMachine != Guid.Empty)
                    {
                        if (LocalRaiserId == resetRequest.TargetMachine.Value)
                            _contentIndexRepository.ResetIndexDirectory();
                    }
                    else
                    {
                        _contentIndexRepository.ResetIndexDirectory();
                    }
                    break;
                case IndexRequestItem.RECOVERINDEX:
                    var recoverRequest = (RecoverIndexRequestItem)request;
                    if (recoverRequest.TargetMachine != null && recoverRequest.TargetMachine != Guid.Empty)
                    {
                        if (LocalRaiserId == recoverRequest.TargetMachine.Value)
                            _indexRecoveryService.RecoverIndex(true);
                    }
                    else
                    {
                        _indexRecoveryService.RecoverIndex(true);
                    }
                    break;
                default:
                    break;
            }
        }

        public void Init()
        {
            if (ShouldRaiseEvent)
            {
                Event @event = this._eventService.Get(IndexContentEventId);
                @event.Raised += new EventNotificationHandler(this.IndexContent_Raised);
            }
        }
    }
}