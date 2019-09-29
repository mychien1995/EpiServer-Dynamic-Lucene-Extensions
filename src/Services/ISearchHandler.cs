using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
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
        SearchResults<T> GetSearchResults<T>(IQueryExpression expression, int pageIndex, int pageSize, SortOptions sortOption = null) where T : DocumentIndexModel;
        TTarget GetContent<TTarget>(DocumentIndexModel indexItem, bool filterOnCulture = true) where TTarget : ContentData;
        List<SearchFacet> GetSearchFacets(IQueryExpression expression, string[] groupByFields);
    }

    [ServiceConfiguration(typeof(ISearchHandler))]
    public class SearchRepository : ISearchHandler
    {
        private readonly IContentRepository _contentRepository;
        public SearchRepository(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }

        public virtual SearchResults<T> GetSearchResults<T>(IQueryExpression expression, int pageIndex, int pageSize, SortOptions sortOption = null) where T : DocumentIndexModel
        {
            var result = new SearchResults<T>();
            var readLock = new ReaderWriterLockSlim();
            readLock.EnterReadLock();
            using (IndexSearcher indexSearcher = new IndexSearcher(LuceneConfiguration.Directory, true))
            {
                Sort luceneSortOption = new Sort();
                if (sortOption != null)
                {
                    luceneSortOption = new Sort(sortOption.Fields.Select(x =>
                    new Lucene.Net.Search.SortField(ContentIndexHelpers.GetIndexFieldName(x.FieldName), x.FieldType, x.Reverse)).ToArray());
                }
                var query = new MultiFieldQueryParser(LuceneConfiguration.LuceneVersion, expression.GetFieldName()
                    , LuceneConfiguration.Analyzer).Parse(expression.GetExpression());
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
            readLock.ExitReadLock();
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

        public virtual List<SearchFacet> GetSearchFacets(IQueryExpression expression, string[] groupByFields)
        {
            groupByFields = groupByFields.Select(x => ContentIndexHelpers.GetIndexFieldName(x)).ToArray();
            var result = new List<SearchFacet>();
            var readLock = new ReaderWriterLockSlim();
            readLock.EnterReadLock();
            using (IndexReader indexReader = IndexReader.Open(LuceneConfiguration.Directory, true))
            {
                var query = new MultiFieldQueryParser(LuceneConfiguration.LuceneVersion, expression.GetFieldName()
                     , LuceneConfiguration.Analyzer).Parse(expression.GetExpression());
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
            readLock.ExitReadLock();
            return result;
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