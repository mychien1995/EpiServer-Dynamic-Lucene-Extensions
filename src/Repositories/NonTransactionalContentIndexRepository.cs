using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer;
using EPiServer.ServiceLocation;
using Lucene.Net.Index;

namespace EPiServer.DynamicLuceneExtensions.Repositories
{
    public class NonTransactionalContentIndexRepository : ContentIndexRepository
    {
        public NonTransactionalContentIndexRepository(IndexWriter writer) : base(new NonTransactionalDocumentRepository(writer)
            , ServiceLocator.Current.GetInstance<IContentRepository>())
        {
        }
    }
}