using EPiServer.DynamicLuceneExtensions.Attributes;

namespace EPiServer.DynamicLuceneExtensions.Models.Search
{
    public class DocumentIndexModel
    {
        [IndexFieldName(Constants.INDEX_FIELD_NAME_ID)]
        public string Id { get; set; }

        [IndexFieldName(Constants.INDEX_FIELD_NAME_ACL)]
        public string ACL { get; set; }

        [IndexFieldName(Constants.INDEX_FIELD_NAME_TYPE)]
        public string ContentType { get; set; }

        [IndexFieldName(Constants.INDEX_FIELD_NAME_NAME)]
        public string Name { get; set; }

        [IndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH)]
        public string VirtualPath { get; set; }

        [IndexFieldName(Constants.INDEX_FIELD_NAME_STATUS)]
        public string ItemStatus { get; set; }

        [IndexFieldName(Constants.INDEX_FIELD_NAME_LANGUAGE)]
        public string Language { get; set; }
    }
}