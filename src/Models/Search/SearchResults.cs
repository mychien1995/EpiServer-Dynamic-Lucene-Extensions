using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EPiServer.DynamicLuceneExtensions.Models.Search
{
    public class SearchResults<T> where T : DocumentIndexModel
    {
        public SearchResults()
        {
            Facets = new List<SearchFacet>();
            Results = new Collection<T>();
        }
        public int TotalHits { get; set; }
        public Collection<T> Results { get; set; }
        public List<SearchFacet> Facets { get; set; }
    }

    public class SearchFacet
    {
        public string Term { get; set; }
        public long Count { get; set; }
    }
}