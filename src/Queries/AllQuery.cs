using EPiServer.DynamicLuceneExtensions.Models.Search;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class AllQuery : IQueryExpression
    {
        public string GetExpression()
        {
            return "*:*";
        }

        public string[] GetFieldName()
        {
            return new string[] { };
        }
    }
}