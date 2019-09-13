namespace EPiServer.DynamicLuceneExtensions
{
    public static class Constants
    {
        public const string INDEX_FIELD_NAME_ID = "ID";
        public const string INDEX_FIELD_NAME_NAME = "NAME";
        public const string INDEX_FIELD_NAME_STATUS = "STATUS";
        public const string INDEX_FIELD_NAME_LANGUAGE = "LANG";
        public const string INDEX_FIELD_NAME_START_PUBLISHDATE = "START_PUBLISH";
        public const string INDEX_FIELD_NAME_STOP_PUBLISHDATE = "STOP_PUBLISH";
        public const string INDEX_FIELD_NAME_CHANGED = "CHANGED";
        public const string INDEX_FIELD_NAME_EXPIRED = "EXPIRED";
        public const string INDEX_FIELD_NAME_CREATED = "CREATED";
        public const string INDEX_FIELD_NAME_REFERENCE = "REFERENCE_ID";
        public const string INDEX_FIELD_NAME_VIRTUAL_PATH = "VIRTUAL_PATH";
        public const string INDEX_FIELD_NAME_ACL = "ACL";
        public const string INDEX_FIELD_NAME_TYPE = "TYPE";
        public const string LUCENE_WHITESPACE_ANALYZER = "Lucene.Net.Analysis.Standard.StandardAnalyzer, Lucene.Net";
        public static class ContainerType
        {
            public const string Azure = "azure";
            public const string FileSystem = "filesystem";
        }
        public const string StringListDelimeter = "|";
    }
}