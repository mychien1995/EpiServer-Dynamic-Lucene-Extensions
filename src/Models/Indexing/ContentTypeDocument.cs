using EPiServer.DynamicLuceneExtensions.Indexing;
using System;
using System.Collections.Generic;

namespace EPiServer.DynamicLuceneExtensions.Models.Indexing
{
    public class ContentTypeDocument
    {
        public ContentTypeDocument()
        {
            IndexedFields = new Dictionary<string, IComputedField>();
        }

        public string ContentTypeName { get; set; }
        public Type ContentType { get; set; }
        public Dictionary<string, IComputedField> IndexedFields { get; set; }
        public bool IndexAllFields { get; set; }
    }
}