using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public interface ISearchHandler
    {
        SearchResults<T> GetSearchResults<T>(IQueryExpression expression, int pageIndex, int pageSize, SortOptions sortOption = null, Directory directory = null) where T : DocumentIndexModel;

        SearchResults<T> GetSearchResults<T>(IQueryExpression expression, int pageIndex, int pageSize, string shardName, SortOptions sortOption = null) where T : DocumentIndexModel;
        List<SearchFacet> GetSearchFacets(IQueryExpression expression, string shardName, string[] groupByFields);
        TTarget GetContent<TTarget>(DocumentIndexModel indexItem, bool filterOnCulture = true) where TTarget : ContentData;
        List<SearchFacet> GetSearchFacets(IQueryExpression expression, string[] groupByFields, Directory directory = null);
    }

    [ServiceConfiguration(typeof(ISearchHandler))]
    public class SearchRepository : ISearchHandler
    {
        private readonly IContentRepository _contentRepository;
        public SearchRepository(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }
        public virtual SearchResults<T> GetSearchResults<T>(IQueryExpression expression, int pageIndex, int pageSize, string shardName, SortOptions sortOption = null) where T : DocumentIndexModel
        {
            if (LuceneContext.IndexShardingStrategy == null) return GetSearchResults<T>(expression, pageIndex, pageSize, sortOption);
            var directory = LuceneContext.IndexShardingStrategy.GetOrCreateShard(shardName)?.Directory;
            if (directory == null) return null;
            return GetSearchResults<T>(expression, pageIndex, pageSize, sortOption, directory);
        }

        public virtual SearchResults<T> GetSearchResults<T>(IQueryExpression expression, int pageIndex, int pageSize, SortOptions sortOption = null, Directory directory = null)
            where T : DocumentIndexModel
        {
            if (directory == null) directory = LuceneContext.Directory;
            var result = new SearchResults<T>();
            using (IndexSearcher indexSearcher = new IndexSearcher(directory, true))
            {
                Sort luceneSortOption = new Sort();
                if (sortOption != null)
                {
                    luceneSortOption = new Sort(sortOption.Fields.Select(x =>
                    new Lucene.Net.Search.SortField(ContentIndexHelpers.GetIndexFieldName(x.FieldName), x.FieldType, x.Reverse)).ToArray());
                }
                var queryParser = new MultiFieldQueryParser(LuceneConfiguration.LuceneVersion, expression.GetFieldName()
                    , LuceneConfiguration.Analyzer);
                queryParser.AllowLeadingWildcard = true;
                var query = queryParser.Parse(expression.GetExpression());
                TopDocs topDocs = indexSearcher.Search(query, null, int.MaxValue, luceneSortOption);
                result.TotalHits = topDocs.TotalHits;
                ScoreDoc[] scoreDocs = topDocs.ScoreDocs;
                var documentParser = new DocumentParser<T>();
                var startIndex = (pageIndex - 1);
                var endIndex = (pageIndex - 1) + pageSize;
                var count = scoreDocs.Count();
                for (int index = startIndex; index < endIndex; index++)
                {
                    if (index >= 0 && index < count)
                    {
                        var document = indexSearcher.Doc(scoreDocs[index].Doc);
                        result.Results.Add(documentParser.ParseFromDocument(document));
                    }
                }
            }
            return result;
        }

        public TTarget GetContent<TTarget>(DocumentIndexModel indexItem, bool filterOnCulture = true) where TTarget : ContentData
        {
            if (indexItem == null || string.IsNullOrEmpty(indexItem.Id))
                return default(TTarget);
            Guid result;
            if (Guid.TryParse(((IEnumerable<string>)indexItem.Id.Split('|')).FirstOrDefault<string>(), out result))
            {
                LoaderOptions loaderOptions;
                if (filterOnCulture)
                {
                    loaderOptions = this.GetLoaderOptions(indexItem.Language);
                }
                else
                {
                    loaderOptions = new LoaderOptions();
                    loaderOptions.Add<LanguageLoaderOption>(LanguageLoaderOption.Fallback((CultureInfo)null));
                }
                LoaderOptions settings = loaderOptions;
                TTarget content = null;
                this._contentRepository.TryGet<TTarget>(result, settings, out content);
                return content;
            }
            return default(TTarget);
        }
        public virtual List<SearchFacet> GetSearchFacets(IQueryExpression expression, string[] groupByFields, Directory directory = null)
        {
            if (directory == null) directory = LuceneContext.Directory;
            groupByFields = groupByFields.Select(x => ContentIndexHelpers.GetIndexFieldName(x)).ToArray();
            var result = new List<SearchFacet>();
            using (IndexReader indexReader = IndexReader.Open(directory, true))
            {
                var queryParser = new MultiFieldQueryParser(LuceneConfiguration.LuceneVersion, expression.GetFieldName()
                     , LuceneConfiguration.Analyzer);
                queryParser.AllowLeadingWildcard = true;
                var query = queryParser.Parse(expression.GetExpression());
                SimpleFacetedSearch facetSearch = new SimpleFacetedSearch(indexReader, groupByFields);
                SimpleFacetedSearch.Hits hits = facetSearch.Search(query, int.MaxValue);
                long totalHits = hits.TotalHitCount;
                foreach (SimpleFacetedSearch.HitsPerFacet hitPerGroup in hits.HitsPerFacet)
                {
                    long hitCountPerGroup = hitPerGroup.HitCount;
                    result.Add(new SearchFacet()
                    {
                        Count = hitPerGroup.HitCount,
                        Term = hitPerGroup.Name.ToString()
                    });
                }
            }
            return result;
        }

        public virtual List<SearchFacet> GetSearchFacets(IQueryExpression expression, string shardName, string[] groupByFields)
        {
            if (LuceneContext.IndexShardingStrategy == null) return GetSearchFacets(expression, groupByFields);
            var directory = LuceneContext.IndexShardingStrategy.GetOrCreateShard(shardName)?.Directory;
            if (directory == null) return null;
            return GetSearchFacets(expression, groupByFields, directory);
        }

        private LoaderOptions GetLoaderOptions(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                LoaderOptions loaderOptions = new LoaderOptions();
                loaderOptions.Add<LanguageLoaderOption>(LanguageLoaderOption.FallbackWithMaster((CultureInfo)null));
                return loaderOptions;
            }
            CultureInfo language = languageCode == "iv" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(languageCode);
            LoaderOptions loaderOptions1 = new LoaderOptions();
            loaderOptions1.Add<LanguageLoaderOption>(LanguageLoaderOption.Specific(language));
            return loaderOptions1;
        }
    }
}