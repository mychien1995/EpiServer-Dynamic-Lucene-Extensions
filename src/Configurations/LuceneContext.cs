using EPiServer.DynamicLuceneExtensions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class LuceneContext
    {
        public static bool AllowIndexing
        {
            get
            {
                if (IndexHealthCheckService.IS_HEALTH_CHECK || IndexRecoveryService.IN_RECOVERING) return false;
                return true;
            }
        }
    }
}