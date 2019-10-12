using EPiServer.ServiceLocation;
using Lucene.Net.Index;
using Lucene.Net.Store.Azure;
using EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend;
using EPiServer.DynamicLuceneExtensions.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public interface IIndexHealthCheckService
    {
        bool IsHealthyIndex(out string message);
    }

    [ServiceConfiguration(typeof(IIndexHealthCheckService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class IndexHealthCheckService : IIndexHealthCheckService
    {
        public static bool IS_HEALTH_CHECK = false;
        private static object _lock = new object();
        public IndexHealthCheckService()
        {
            //make sure that no indexing is occured before this turned off
            //IS_HEALTH_CHECK = false;
        }
        public bool IsHealthyIndex(out string message)
        {
            message = "";
            if (IS_HEALTH_CHECK) return true;
            lock (_lock)
            {
                if (IS_HEALTH_CHECK) return true;
                IS_HEALTH_CHECK = true;
            }
            try
            {
                if (IndexRecoveryService.IN_RECOVERING)
                {
                    IS_HEALTH_CHECK = false;
                    return true;
                }
                var directory = LuceneConfiguration.Directory;
                if (directory == null || directory is FastAzureDirectory || directory is AzureDirectory)
                {
                    message = "Can't perform index checked on blob storage";
                    IS_HEALTH_CHECK = false;
                    return false;
                }
                if (IndexWriter.IsLocked(directory))
                {
                    IndexWriter.Unlock(directory);
                }
                var checkIndex = new CheckIndex(directory);
                if (!checkIndex.CheckIndex_Renamed_Method().clean)
                {
                    message = "Broken index";
                    IS_HEALTH_CHECK = false;
                    return false;
                }
                IS_HEALTH_CHECK = false;
            }
            catch (Exception ex)
            {
                IS_HEALTH_CHECK = false;
                throw ex;
            }
            return true;
        }
    }
}