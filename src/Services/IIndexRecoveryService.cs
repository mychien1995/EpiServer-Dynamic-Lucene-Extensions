using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.DynamicLuceneExtensions.Business.Indexing;
using EPiServer.DynamicLuceneExtensions.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public interface IIndexRecoveryService
    {
        void RecoverIndex(bool manualForce);
    }

    [ServiceConfiguration(typeof(IIndexRecoveryService))]
    public class IndexRecoveryService : IIndexRecoveryService
    {
        public static bool IN_RECOVERING = false;
        private readonly IContentIndexRepository _contentIndexRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(IndexRecoveryService));
        private static object _lock = new object();
        public IndexRecoveryService(ISiteDefinitionRepository siteDefinitionRepository, IContentRepository contentRepository, IContentIndexRepository contentIndexRepository)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _contentRepository = contentRepository;
            _contentIndexRepository = contentIndexRepository;
        }
        public void RecoverIndex(bool manualForce)
        {
            if (IN_RECOVERING) return;
            lock (_lock)
            {
                if (IN_RECOVERING) return;
                IN_RECOVERING = true;
            }
            try
            {
                _logger.Error("Lucene: Recovering Index");
                _contentIndexRepository.ResetIndexDirectory();
                var siteRoots = GetAllSiteRoots();
                foreach (var siteId in siteRoots)
                {
                    PageData siteRootPage;
                    if (_contentRepository.TryGet<PageData>(new ContentReference(siteId), out siteRootPage))
                    {
                        _contentIndexRepository.ReindexSiteForRecovery(siteRootPage);
                    }
                }
                IN_RECOVERING = false;
                _logger.Error("Lucene: Done Recovering");
            }
            catch (Exception ex)
            {
                IN_RECOVERING = false;
                throw ex;
            }
        }

        private List<int> GetAllSiteRoots()
        {
            var siteList = _siteDefinitionRepository.List().Select(x => x.StartPage.ID).ToList();
            return siteList;
        }
    }
}