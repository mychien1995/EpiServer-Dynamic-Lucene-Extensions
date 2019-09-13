using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace EPiServer.DynamicLuceneExtensions.Repositories
{
    public interface IDocumentRepository
    {
        Document GetDocumentById(string id);
        Collection<SearchDocument> SearchByField(string fieldname, string value, int maxHits, out int totalHits);
        void WriteToIndex(SearchDocument document);
        void DeleteFromIndex(List<string> itemIds);
        void Optimize();
        void UpdateBatchIndex(List<SearchDocument> documents);
        void ReindexSite(List<SearchDocument> documents, Guid siteRootId);

        long GetIndexFolderSize(string folderPath);
    }

    [ServiceConfiguration(typeof(IDocumentRepository))]
    public class DocumentRepository : IDocumentRepository
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(DocumentRepository));
        private static object _writeLock = new object();
        public virtual Document GetDocumentById(string id)
        {
            int totalHits = 0;
            Collection<SearchDocument> collection = this.SearchByField(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ID)
                , QueryParser.Escape(id), 1, out totalHits);
            if (collection.Count > 0)
                return collection[0].Document;
            return null;
        }
        public virtual long GetIndexFolderSize(string folderPath)
        {
            if (System.IO.Directory.Exists(folderPath))
            {
                DirectoryInfo dir = new DirectoryInfo(folderPath);
                dir.Refresh();
                long size = 0;
                string[] files = System.IO.Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    if (!File.Exists(file)) continue;
                    FileInfo details = new FileInfo(file);
                    if (details != null)
                    {
                        details.Refresh();
                        size += details.Length;
                    }
                }
                return size;
            }
            return -1;
        }

        public virtual Collection<SearchDocument> SearchByField(string fieldname, string value, int maxHits, out int totalHits)
        {
            Collection<SearchDocument> collection = new Collection<SearchDocument>();
            totalHits = 0;
            try
            {
                Query query = new QueryParser(LuceneConfiguration.LuceneVersion, fieldname, LuceneConfiguration.Analyzer).Parse(value);
                using (IndexSearcher indexSearcher = new IndexSearcher(LuceneConfiguration.Directory, true))
                {
                    TopDocs topDocs = indexSearcher.Search(query, maxHits);
                    totalHits = topDocs.TotalHits;
                    ScoreDoc[] scoreDocs = topDocs.ScoreDocs;
                    for (int index = 0; index < scoreDocs.Length; ++index)
                    {
                        var document = indexSearcher.Doc(scoreDocs[index].Doc);
                        collection.Add(new SearchDocument(document.GetField(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ID)).StringValue, document));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Lucene Search Error", ex);
            }
            return collection;
        }

        public virtual void WriteToIndex(SearchDocument document)
        {
            try
            {
                lock (_writeLock)
                {
                    using (IndexWriter indexWriter = new IndexWriter(LuceneConfiguration.Directory, LuceneConfiguration.Analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        indexWriter.AddDocument(document.Document);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Lucene Search Error", ex);
            }
        }

        public virtual void DeleteFromIndex(List<string> itemIds)
        {
            try
            {
                lock (_writeLock)
                {
                    BooleanQuery.MaxClauseCount = int.MaxValue;
                    var deleteQueries = new List<Query>();
                    foreach (var deletedDoc in itemIds)
                    {
                        var deleteQuery = new QueryParser(LuceneConfiguration.LuceneVersion, ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ID), LuceneConfiguration.Analyzer)
                            .Parse(deletedDoc);
                        deleteQueries.Add(deleteQuery);
                    }
                    using (IndexWriter indexWriter = new IndexWriter(LuceneConfiguration.Directory, LuceneConfiguration.Analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        indexWriter.SetMergeScheduler(new SerialMergeScheduler());
                        indexWriter.DeleteDocuments(deleteQueries.ToArray());
                        indexWriter.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Lucene Search Error", ex);
            }
        }

        public virtual void UpdateBatchIndex(List<SearchDocument> documents)
        {
            var deletedList = new List<SearchDocument>();
            var itemIds = documents.Select(x => x.Id).ToList();
            try
            {
                lock (_writeLock)
                {
                    BooleanQuery.MaxClauseCount = int.MaxValue;
                    var deleteQueries = new List<Query>();
                    foreach (var deletedDoc in itemIds)
                    {
                        var deleteQuery = new QueryParser(LuceneConfiguration.LuceneVersion, ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ID), LuceneConfiguration.Analyzer)
                            .Parse(deletedDoc);
                        deleteQueries.Add(deleteQuery);
                    }
                    using (IndexWriter indexWriter = new IndexWriter(LuceneConfiguration.Directory, LuceneConfiguration.Analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        indexWriter.SetMergeScheduler(new SerialMergeScheduler());
                        indexWriter.DeleteDocuments(deleteQueries.ToArray());
                        foreach (var document in documents)
                        {
                            indexWriter.AddDocument(document.Document);
                        }
                        indexWriter.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Lucene Search Error", ex);
            }
        }

        public virtual void ReindexSite(List<SearchDocument> documents, Guid siteRootId)
        {
            var deletedList = new List<SearchDocument>();
            try
            {
                lock (_writeLock)
                {
                    BooleanQuery.MaxClauseCount = int.MaxValue;
                    var fieldName = ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH);
                    var siteRoot = "*" + LuceneQueryHelper.Escape(siteRootId.ToString().ToLower().Replace(" ", "")) + "*";
                    var siteRootQuery = $"{fieldName}:{siteRoot}";
                    var queryParser = new QueryParser(LuceneConfiguration.LuceneVersion, fieldName, LuceneConfiguration.Analyzer);
                    queryParser.AllowLeadingWildcard = true;
                    var deleteQuery = queryParser.Parse(siteRootQuery);
                    using (IndexWriter indexWriter = new IndexWriter(LuceneConfiguration.Directory, LuceneConfiguration.Analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        indexWriter.SetMergeScheduler(new SerialMergeScheduler());
                        indexWriter.DeleteDocuments(deleteQuery);
                        foreach (var document in documents)
                        {
                            indexWriter.AddDocument(document.Document);
                        }
                        indexWriter.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Lucene Search Error", ex);
            }
        }

        public virtual void Optimize()
        {
            try
            {
                lock (_writeLock)
                {
                    using (IndexWriter indexWriter = new IndexWriter(LuceneConfiguration.Directory, LuceneConfiguration.Analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        indexWriter.Optimize();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Lucene Search Error", ex);
            }
        }
    }
}