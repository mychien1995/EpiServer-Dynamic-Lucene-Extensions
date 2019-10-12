using EPiServer;
using EPiServer.Core;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using EPiServer.DynamicLuceneExtensions.Repositories;
using EPiServer.DynamicLuceneExtensions.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Business.Indexing
{
    public interface ISiteIndexHandler
    {
        string IndexSite(List<int> siteStartPageIds);
    }

    [ServiceConfiguration(typeof(ISiteIndexHandler))]
    public class LuceneSiteIndexHandler : ISiteIndexHandler
    {
        private readonly IContentRepository _contentRepository;
        private readonly IIndexingHandler _indexingHandler;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILog _logger = LogManager.GetLogger(typeof(LuceneSiteIndexHandler));
        public LuceneSiteIndexHandler(IContentRepository contentRepository
            , IDocumentRepository documentRepository, IIndexingHandler indexingHandler)
        {
            _contentRepository = contentRepository;
            _indexingHandler = indexingHandler;
            _documentRepository = documentRepository;
        }

        public string IndexSite(List<int> siteStartPageIds)
        {
            var progress = new StringBuilder();
            progress.AppendLine("Start Indexing </br>");
            if (!LuceneConfiguration.Active)
            {
                progress.AppendLine("Lucene not activated </br>");
                return progress.ToString();
            }
            foreach (var siteId in siteStartPageIds)
            {
                PageData siteRoot;
                if (_contentRepository.TryGet<PageData>(new ContentReference(siteId), out siteRoot))
                {
                    if (siteRoot == null) continue;
                    try
                    {
                        SlimContentReader slimContentReader = new SlimContentReader(this._contentRepository, siteRoot.ContentLink, (c =>
                        {
                            return true;
                        }));
                        while (slimContentReader.Next())
                        {
                            var currentContent = slimContentReader.Current;
                            if (currentContent != null && !currentContent.ContentLink.CompareToIgnoreWorkID(ContentReference.RootPage))
                            {
                                if (LuceneConfiguration.CanIndexContent(currentContent))
                                {
                                    _indexingHandler.ProcessRequest(new IndexRequestItem(currentContent));
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Lucene Index Site Error: {siteRoot.ContentLink.ID} - {siteRoot.Name}", e);
                        progress.AppendLine($"{siteRoot.ContentLink.ID} - {siteRoot.Name} index failed </br>");
                        continue;
                    }
                    progress.AppendLine($"Site: {siteRoot.ContentLink.ID} - {siteRoot.Name} indexed; </br>");
                }
            }
            progress.AppendLine("Indexing completed</br>");
            return progress.ToString();
        }
    }
}