using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Repositories;
using EPiServer.DynamicLuceneExtensions.Services;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using System;
using System.Threading.Tasks;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace EPiServer.DynamicLuceneExtensions.Initializations
{
    /// <summary>
    /// TODO: Will move this into a shared lib later!
    /// </summary>
    public abstract class SiteCreationServiceBase
    {
        public static string CreationServiceKey => "ISiteCreationService_CreationServiceKey";

        public static bool IsSettingUpSite()
        {
            try
            {
                var result = CacheManager.Get(CreationServiceKey) as bool?;
                return result.HasValue ? result.Value : false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    [InitializableModule]
    [ModuleDependency(typeof(InitializationModule))]
    public class LuceneInitialization : IInitializableModule
    {
        private static object _lock = new object();
        private readonly Lazy<IContentIndexRepository> _contentIndexRepository = new Lazy<IContentIndexRepository>(() => ServiceLocator.Current.GetInstance<IContentIndexRepository>());
        private readonly Lazy<IRemoteContentIndexRepository> _remoteContentIndexRepository = new Lazy<IRemoteContentIndexRepository>(() => ServiceLocator.Current.GetInstance<IRemoteContentIndexRepository>());
        private readonly Lazy<IContentRepository> _contentRepository = new Lazy<IContentRepository>(() => ServiceLocator.Current.GetInstance<IContentRepository>());
        private readonly Lazy<IIndexingHandler> _indexingHandler = new Lazy<IIndexingHandler>(() => ServiceLocator.Current.GetInstance<IIndexingHandler>());

        public LuceneInitialization()
        {

        }

        public void Initialize(InitializationEngine context)
        {
            if (!LuceneConfiguration.Active) return;
            IContentEvents contentEvents = context.Locate.ContentEvents();
            IContentSecurityRepository securityRepository = context.Locate.ContentSecurityRepository();
            contentEvents.PublishedContent += new EventHandler<ContentEventArgs>(PublishContent);
            contentEvents.MovedContent += new EventHandler<ContentEventArgs>(MovedContent);
            contentEvents.DeletedContent += new EventHandler<DeleteContentEventArgs>(DeleteContent);
            contentEvents.DeletedContentLanguage += new EventHandler<ContentEventArgs>(DeleteContentLanguage);
            securityRepository.ContentSecuritySaved += new EventHandler<ContentSecurityEventArg>(UpdateSecurity);
            _indexingHandler.Value.Init();
            _remoteContentIndexRepository.Value.Init();
            DoingHealthCheck();
        }

        public void Uninitialize(InitializationEngine context)
        {
            if (!LuceneConfiguration.Active) return;
            IContentEvents contentEvents = context.Locate.ContentEvents();
            IContentSecurityRepository securityRepository = context.Locate.ContentSecurityRepository();
            contentEvents.PublishedContent -= new EventHandler<ContentEventArgs>(PublishContent);
            contentEvents.MovedContent -= new EventHandler<ContentEventArgs>(MovedContent);
            contentEvents.DeletedContent -= new EventHandler<DeleteContentEventArgs>(DeleteContent);
            contentEvents.DeletedContentLanguage -= new EventHandler<ContentEventArgs>(DeleteContentLanguage);
            securityRepository.ContentSecuritySaved -= new EventHandler<ContentSecurityEventArg>(UpdateSecurity);
        }
        private void DoingHealthCheck()
        {
            Task.Run(() =>
            {
                var indexHealthCheckService = ServiceLocator.Current.GetInstance<IIndexHealthCheckService>();
                var indexRecoveryService = ServiceLocator.Current.GetInstance<IIndexRecoveryService>();
                string message;
                if (!indexHealthCheckService.IsHealthyIndex(out message))
                {
                    indexRecoveryService.RecoverIndex(true);
                }
            });
        }
        private void PublishContent(object sender, ContentEventArgs e)
        {
            Task.Run(() =>
            {
                if (SiteCreationServiceBase.IsSettingUpSite()) return;
                if (LuceneConfiguration.CanIndexContent(e.Content))
                    _indexingHandler.Value.ProcessRequest(new IndexRequestItem(e.Content));
            });

        }

        private void MovedContent(object sender, ContentEventArgs e)
        {
            Task.Run(() =>
            {
                if (SiteCreationServiceBase.IsSettingUpSite()) return;
                if (LuceneConfiguration.CanIndexContent(e.Content))
                    _indexingHandler.Value.ProcessRequest(new IndexRequestItem(e.Content));
            });
        }

        private void UpdateSecurity(object sender, ContentSecurityEventArg e)
        {
            Task.Run(() =>
            {
                if (SiteCreationServiceBase.IsSettingUpSite()) return;
                var content = _contentRepository.Value.Get<IContent>(e.ContentLink);
                if (LuceneConfiguration.CanIndexContent(content))
                    _indexingHandler.Value.ProcessRequest(new IndexRequestItem(content));
            });
        }

        private void DeleteContent(object sender, ContentEventArgs e)
        {
            Task.Run(() =>
            {
                if (SiteCreationServiceBase.IsSettingUpSite()) return;
                if (LuceneConfiguration.CanIndexContent(e.Content))
                    _indexingHandler.Value.ProcessRequest(new IndexRequestItem(e.Content, IndexRequestItem.REMOVE, true));
            });
        }

        private void DeleteContentLanguage(object sender, ContentEventArgs e)
        {
            Task.Run(() =>
            {
                if (SiteCreationServiceBase.IsSettingUpSite()) return;
                if (LuceneConfiguration.CanIndexContent(e.Content))
                    _indexingHandler.Value.ProcessRequest(new IndexRequestItem(e.Content, IndexRequestItem.REMOVE_LANGUAGE));
            });
        }
    }
}