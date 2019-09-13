using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Business.ScheduledJob;
using System.Collections.Generic;

namespace EPiServer.DynamicLuceneExtensions.Business.ScheduledJobRunners
{
    public class LuceneSiteIndexJobRunner
    {
        public static List<int> SiteIds;

        public void RunIndexing(List<int> siteIds)
        {
            SiteIds = siteIds;
            var repo = ServiceLocator.Current.GetInstance<IScheduledJobRepository>();
            var pluginDescriptor = PlugInDescriptor.Load(typeof(LuceneSiteIndexScheduledJob));
            var job = repo.Get("Execute", pluginDescriptor.TypeName, pluginDescriptor.AssemblyName);
            if (job != null)
            {
                ServiceLocator.Current.GetInstance<IScheduledJobExecutor>().StartAsync(job);
            }
        }

    }
}