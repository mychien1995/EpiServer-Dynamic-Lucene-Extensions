using EPiServer.Core;
using EPiServer.Events.Clients;
using EPiServer.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using EPiServer.ServiceLocation;

namespace EPiServer.DynamicLuceneExtensions.Repositories
{
    public interface IRemoteContentIndexRepository
    {
        void IndexContent(IContent content, bool includeChild = false);
        void RemoveContentIndex(IContent content, bool includeChild = false);
        void ReindexSite(IContent content);

        void ResetIndexDirectory(string folderPath);
    }

    [ServiceConfiguration(typeof(IRemoteContentIndexRepository))]
    public class RemoteContentIndexRepository : IRemoteContentIndexRepository
    {
        public static readonly Guid LocalRaiserId = new Guid("{f9eb6d50-f831-4598-b5cb-852236cb5fde}");
        public static readonly Guid IndexContentEventId = new Guid("{e5a66653-83fe-4de1-b69b-26c5134685ea}");
        private readonly IEventRegistry _eventService;
        private readonly IContentIndexRepository _contentIndexRepository;
        public RemoteContentIndexRepository(IContentIndexRepository contentIndexRepository, IEventRegistry eventService)
        {
            _contentIndexRepository = contentIndexRepository;
            _eventService = eventService;
            Event @event = this._eventService.Get(IndexContentEventId);
            @event.Raised += new EventNotificationHandler(this.IndexContent_Raised);
        }
        public void IndexContent(IContent content, bool includeChild = false)
        {
            _contentIndexRepository.IndexContent(content, includeChild);
            var indexRequest = new IndexRequestItem(content, IndexRequestItem.REINDEX, includeChild);
            var param = indexRequest.RemoteRequest;
            this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
        }

        public void RemoveContentIndex(IContent content, bool includeChild = false)
        {
            _contentIndexRepository.RemoveContentIndex(content, includeChild);
            var indexRequest = new IndexRequestItem(content, IndexRequestItem.REMOVE, includeChild);
            var param = indexRequest.RemoteRequest;
            this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
        }


        public void ReindexSite(IContent content)
        {
            _contentIndexRepository.ReindexSite(content);
            var indexRequest = new IndexRequestItem(content, IndexRequestItem.REINDEXSITE, true);
            var param = indexRequest.RemoteRequest;
            this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
        }
        public virtual void ResetIndexDirectory(string folderPath)
        {
            _contentIndexRepository.ResetIndexDirectory(folderPath);
            var param = new ResetIndexRequestItem(folderPath).RemoteRequest;
            this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, param, EventRaiseOption.RaiseBroadcast);
        }

        private void IndexContent_Raised(object sender, EventNotificationEventArgs e)
        {
            if (!(e.RaiserId != LocalRaiserId))
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
                case IndexRequestItem.RESETINDEX:
                    var resetRequest = (ResetIndexRequestItem)request;
                    _contentIndexRepository.ResetIndexDirectory(resetRequest.FolderPath);
                    break;
                default:
                    break;
            }
        }
    }
}