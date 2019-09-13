using EPiServer.Core;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using EPiServer.DynamicLuceneExtensions.Queries;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public class QueryBuilder
    {
        public static IQueryExpression BuildQuery<T>(bool includeInherit = false) where T : ContentData
        {
            return new ContentTypeQuery<T>(includeInherit);
        }
    }
}