namespace EPiServer.DynamicLuceneExtensions.Models.Search
{
    public interface IQueryExpression
    {
        string GetExpression();
        string[] GetFieldName();
    }
}