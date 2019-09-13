using Lucene.Net.Documents;
using EPiServer.DynamicLuceneExtensions.Attributes;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System;
using System.ComponentModel;
using System.Linq;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public class DocumentParser<T> where T : DocumentIndexModel
    {
        public T ParseFromDocument(Document document)
        {
            var instance = Activator.CreateInstance<T>();
            var indexedProperties = typeof(T).GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(IndexFieldNameAttribute), true).Any())
                .ToList();
            foreach (var property in indexedProperties)
            {
                var indexFieldNameAttrs = (IndexFieldNameAttribute[])property.GetCustomAttributes(typeof(IndexFieldNameAttribute), true);
                var indexFieldName = ContentIndexHelpers.GetIndexFieldName(indexFieldNameAttrs.FirstOrDefault().IndexFieldName);
                var documentField = document.GetField(indexFieldName);
                if (documentField != null)
                {
                    var fieldValue = documentField.StringValue;
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(property.PropertyType);
                    object propValue = typeConverter.ConvertFromString(fieldValue);
                    property.SetValue(instance, propValue, null);
                }
            }
            return instance;
        }
    }
}