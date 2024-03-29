﻿using EPiServer.ServiceLocation;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Models;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using EPiServer.DynamicLuceneExtensions.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public interface IIndexingHandler
    {
        void Init();
        void ProcessRequest(IndexRequestItem request);

        void ProcessRequests(IEnumerable<IndexRequestItem> request);
    }

    public class IndexingHandler : IIndexingHandler
    {
        private readonly IContentIndexRepository _contentIndexRepository;
        private readonly IRemoteContentIndexRepository _remoteContentIndexRepository;
        public IndexingHandler()
        {
            _contentIndexRepository = ServiceLocator.Current.GetInstance<IContentIndexRepository>();
            _remoteContentIndexRepository = ServiceLocator.Current.GetInstance<IRemoteContentIndexRepository>();
        }
        public IndexingHandler(IContentIndexRepository contentIndexRepository, IRemoteContentIndexRepository
            remoteContentIndexRepository)
        {
            _contentIndexRepository = contentIndexRepository;
            _remoteContentIndexRepository = remoteContentIndexRepository;
        }

        public void Init()
        {

        }

        public void ProcessRequest(IndexRequestItem request)
        {
            if (string.IsNullOrEmpty(request?.Action) || request?.Content == null) return;
            switch (request.Action)
            {
                case IndexRequestItem.REINDEX:
                    _remoteContentIndexRepository.IndexContent(request.Content, request.IncludeChild);
                    break;
                case IndexRequestItem.REMOVE:
                    _remoteContentIndexRepository.RemoveContentIndex(request.Content, request.IncludeChild);
                    break;
                case IndexRequestItem.REMOVE_LANGUAGE:
                    _remoteContentIndexRepository.RemoveContentIndex(request.Content, false);
                    break;
                case IndexRequestItem.REINDEXSITE:
                    _remoteContentIndexRepository.ReindexSite(request.Content);
                    break;
                default:
                    break;
            }
        }
        public void ProcessRequests(IEnumerable<IndexRequestItem> requests)
        {
            if (!LuceneContext.AllowIndexing) return;
            if (LuceneContext.IndexShardingStrategy == null)
                ProcessDirectoryRequests(requests, LuceneContext.Directory);
            else ProcessShardRequests(requests);
        }
        public void ProcessDirectoryRequests(IEnumerable<IndexRequestItem> requests, Directory directory)
        {
            using (IndexWriter indexWriter = new IndexWriter(directory, LuceneConfiguration.Analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                try
                {
                    indexWriter.SetMergeScheduler(new SerialMergeScheduler());
                    indexWriter.SetRAMBufferSizeMB(2000);
                    if (directory is FastAzureDirectory || directory is AzureDirectory)
                        indexWriter.UseCompoundFile = false;
                    var indexRepository = new NonTransactionalContentIndexRepository(indexWriter);
                    foreach (var request in requests)
                    {
                        if (string.IsNullOrEmpty(request?.Action) || request?.Content == null) return;
                        switch (request.Action)
                        {
                            case IndexRequestItem.REINDEX:
                                indexRepository.IndexContent(request.Content, request.IncludeChild);
                                break;
                            case IndexRequestItem.REMOVE:
                                indexRepository.RemoveContentIndex(request.Content, request.IncludeChild);
                                break;
                            case IndexRequestItem.REMOVE_LANGUAGE:
                                indexRepository.RemoveContentIndex(request.Content, false);
                                break;
                            case IndexRequestItem.REINDEXSITE:
                                indexRepository.ReindexSite(request.Content);
                                break;
                            default:
                                break;
                        }
                    }
                    indexWriter.Commit();
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (IndexWriter.IsLocked(directory))
                    {
                        IndexWriter.Unlock(directory);
                    }
                }
            }
        }
        public void ProcessShardRequests(IEnumerable<IndexRequestItem> requests)
        {
            var strategy = LuceneContext.IndexShardingStrategy;
            var batchs = new Dictionary<IndexShard, List<IndexRequestItem>>();
            foreach (var request in requests)
            {
                var shard = strategy.LocateShard(request);
                if (shard != null)
                {
                    List<IndexRequestItem> shardRequests;
                    if (batchs.TryGetValue(shard, out shardRequests))
                    {
                        shardRequests.Add(request);
                    }
                    else
                    {
                        batchs.Add(shard, new List<IndexRequestItem>() { request });
                    }
                }
            }
            var taskList = new List<Task>();
            foreach (var item in batchs)
            {
                taskList.Add(Task.Run(() =>
                {
                    ProcessDirectoryRequests(item.Value, item.Key.Directory);
                }));
            }
            Task.WaitAll(taskList.ToArray());
        }
    }
}