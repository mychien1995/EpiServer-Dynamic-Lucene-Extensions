using EPiServer.Core;
using System;

namespace EPiServer.DynamicLuceneExtensions.Indexing.ComputedFields
{
    public class BooleanField : IndexableComputedField
    {
        public override object GetValue(IContent content, string fieldName)
        {
            if (content == null || !(content is PageData)) return null;
            var page = content as PageData;
            var property = content.GetType().GetProperty(fieldName);
            if (property != null && (property.PropertyType == typeof(bool) || property.PropertyType == typeof(Boolean)
                || property.PropertyType == typeof(bool?) || property.PropertyType == typeof(Boolean?)))
            {
                var value = property.GetValue(content, null);
                if (value != null && bool.TryParse(value + "", out var boolValue))
                {
                    return boolValue.ToString().ToLower();
                }
            }
            return null;
        }
    }
}