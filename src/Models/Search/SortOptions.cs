using System.Collections.Generic;

namespace EPiServer.DynamicLuceneExtensions.Models.Search
{
    public class SortOptions
    {
        public SortOptions()
        {
            Fields = new List<SortField>();
        }
        public List<SortField> Fields { get; set; }
    }

    public class SortField
    {
        public string FieldName { get; set; }
        public bool Reverse { get; set; }
        public int FieldType { get; set; }
        public SortField(string fieldName, int fieldType, bool isDesc)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            Reverse = isDesc;
        }


        public const int SCORE = 0;
        public const int DOC = 1;
        public const int STRING = 3;
        public const int INT = 4;
        public const int FLOAT = 5;
        public const int LONG = 6;
        public const int DOUBLE = 7;
        public const int SHORT = 8;
        public const int CUSTOM = 9;
        public const int BYTE = 10;
        public const int STRING_VAL = 11;
    }
}