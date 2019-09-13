using EPiServer.Core;

namespace EPiServer.DynamicLuceneExtensions.Indexing.ComputedFields
{
    public class DefaultComputedField : IndexableComputedField
    {
        public override object GetValue(IContent content, string fieldName)
        {
            var property = content.GetType().GetProperty(fieldName);
            if (property != null)
            {
                return property.GetValue(content, null);
            }
            return null;
        }
    }
}