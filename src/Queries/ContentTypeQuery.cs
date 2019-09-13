using EPiServer.Core;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class ContentTypeQuery<T> : IQueryExpression where T : IContentData
    {
        private readonly bool IncludeInheritances;
        public ContentTypeQuery(bool includeInherit = false)
        {
            IncludeInheritances = includeInherit;
        }
        public string GetExpression()
        {
            return new FieldQuery(Constants.INDEX_FIELD_NAME_TYPE, typeof(T).FullName + "*").GetExpression();
        }

        public string[] GetFieldName()
        {
            return new string[] { ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_TYPE) };
        }
    }

    public class ContentTypeQuery : IQueryExpression
    {
        private readonly bool IncludeInheritances;
        private readonly Type Type;
        public ContentTypeQuery(Type type, bool includeInherit = false)
        {
            IncludeInheritances = includeInherit;
            Type = type;
        }
        public string GetExpression()
        {
            return new FieldQuery(Constants.INDEX_FIELD_NAME_TYPE, Type.FullName + "*").GetExpression();
        }

        public string[] GetFieldName()
        {
            return new string[] { ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_TYPE) };
        }
    }
}