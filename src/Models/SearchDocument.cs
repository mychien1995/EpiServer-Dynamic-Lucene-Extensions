using EPiServer.Core;
using Lucene.Net.Documents;

namespace EPiServer.DynamicLuceneExtensions.Models
{
    public class SearchDocument
    {
        public string Id { get; set; }
        public Document Document { get; set; }
        public SearchDocument()
        {

        }
        public SearchDocument(string id, Document doc)
        {
            Id = id;
            Document = doc;
        }

        public static string FormatDocumentId(IContent content)
        {
            var localizable = content as ILocalizable;
            return content.ContentGuid.ToString().ToLower() + "|" + localizable.Language.Name;
        }
    }
}