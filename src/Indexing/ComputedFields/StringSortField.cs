using EPiServer.Core;

namespace EPiServer.DynamicLuceneExtensions.Indexing.ComputedFields
{
    public class StringSortField : IndexableComputedField
    {
        public override object GetValue(IContent content, string fieldName)
        {
            if (!fieldName.EndsWith("_Sort")) return null;
            fieldName = fieldName.Replace("_Sort", "");
            var property = content.GetType().GetProperty(fieldName);
            if (property != null)
            {
                return property.GetValue(content, null);
            }
            return null;
        }
    }
}