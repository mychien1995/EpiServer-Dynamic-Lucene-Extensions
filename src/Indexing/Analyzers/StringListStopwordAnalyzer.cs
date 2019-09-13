using Lucene.Net.Analysis.Standard;
using EPiServer.DynamicLuceneExtensions.Configurations;
using System.Collections.Generic;

namespace EPiServer.DynamicLuceneExtensions.Indexing.Analyzers
{
    public class StringListStopwordAnalyzer : StandardAnalyzer
    {
        public StringListStopwordAnalyzer() : base(LuceneConfiguration.LuceneVersion, new HashSet<string> { Constants.StringListDelimeter })
        {
        }
    }
}