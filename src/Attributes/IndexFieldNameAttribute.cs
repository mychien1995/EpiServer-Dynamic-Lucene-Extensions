using System;

namespace EPiServer.DynamicLuceneExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexFieldNameAttribute : Attribute
    {
        public string IndexFieldName { get; set; }
        public IndexFieldNameAttribute(string indexFieldName)
        {
            IndexFieldName = indexFieldName;
        }
    }
}