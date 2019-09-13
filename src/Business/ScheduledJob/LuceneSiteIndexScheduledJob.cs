using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Business.Indexing;
using EPiServer.DynamicLuceneExtensions.Business.ScheduledJobRunners;
using System.Linq;

namespace EPiServer.DynamicLuceneExtensions.Business.ScheduledJob
{
    [ScheduledPlugIn(DisplayName = "Lucene Site Content Indexing Job")]
    public class LuceneSiteIndexScheduledJob : ScheduledJobBase
    {
        public override string Execute()
        {
            var siteIds = LuceneSiteIndexJobRunner.SiteIds;
            if (siteIds == null || !siteIds.Any()) return "Please select site to re-index through Lucene Site Index Job Runner";
            var service = ServiceLocator.Current.GetInstance<ISiteIndexHandler>();
            var result = service.IndexSite(siteIds);
            LuceneSiteIndexJobRunner.SiteIds = null;
            return result;
        }
    }
}