using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class IndexShardingElement : ConfigurationElement
    {
        [ConfigurationProperty("strategy")]
        public string Strategy
        {
            get
            {
                return (string)this["strategy"];
            }
            set
            {
                this["strategy"] = (object)value;
            }
        }
    }
}