using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models;

namespace EPiServer.DynamicLuceneExtensions.Repositories
{
    public class NonTransactionalDocumentRepository : DocumentRepository
    {
        private IndexWriter _indexWriter;
        private static object _writeLock = new object();
        public NonTransactionalDocumentRepository(IndexWriter writer)
        {
            _indexWriter = writer;
        }

        public override void UpdateBatchIndex(List<SearchDocument> documents)
        {
            var deletedList = new List<SearchDocument>();
            var itemIds = documents.Select(x => x.Id).ToList();
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
                _indexWriter.DeleteDocuments(deleteQueries.ToArray());
                foreach (var document in documents)
                {
                    _indexWriter.AddDocument(document.Document);
                }
            }
        }

        public override void DeleteFromIndex(List<string> itemIds)
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
                _indexWriter.DeleteDocuments(deleteQueries.ToArray());
            }
        }
    }
}