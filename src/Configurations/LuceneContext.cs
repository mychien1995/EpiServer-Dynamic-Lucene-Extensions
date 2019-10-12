using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend;
using EPiServer.DynamicLuceneExtensions.Models;
using EPiServer.DynamicLuceneExtensions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class LuceneContext
    {
        public static string DirectoryType
        {
            get
            {
                return ConfigurationManager.AppSettings["lucene:DirectoryType"];
            }
        }
        public static bool AllowIndexing
        {
            get
            {
                if (IndexHealthCheckService.IS_HEALTH_CHECK || IndexRecoveryService.IN_RECOVERING) return false;
                return true;
            }
        }
        public static bool ShouldRaiseRemoteEvent
        {
            get
            {
                return LuceneConfiguration.Directory != null && LuceneConfiguration.Directory is FSDirectory;
            }
        }

        public static Directory Directory
        {
            get
            {
                return LuceneConfiguration.Directory;
            }
        }

        public static IIndexShardingStrategy IndexShardingStrategy;
    }
}