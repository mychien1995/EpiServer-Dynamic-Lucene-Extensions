using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using EPiServer.DynamicLuceneExtensions.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public interface IIndexingHandler
    {
        void ProcessRequest(IndexRequestItem request);
    }
    [ServiceConfiguration(typeof(IIndexingHandler))]
    public class IndexingHandler : IIndexingHandler
    {
        private readonly IContentIndexRepository _contentIndexRepository;
        private readonly IRemoteContentIndexRepository _remoteContentIndexRepository;
        public IndexingHandler(IContentIndexRepository contentIndexRepository, IRemoteContentIndexRepository
            remoteContentIndexRepository)
        {
            _contentIndexRepository = contentIndexRepository;
            _remoteContentIndexRepository = remoteContentIndexRepository;
        }
        //TODO: We might use a queue for this later
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
    }
}