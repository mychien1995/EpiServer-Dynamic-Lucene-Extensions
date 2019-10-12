using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Models
{
    public class IndexShard
    {
        public string Name { get; set; }
        public Directory Directory { get; set; }
    }
}