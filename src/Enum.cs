namespace EPiServer.DynamicLuceneExtensions
{
    public enum LuceneFieldType
    {
        String,
        Datetime,
        Numeric,
        Multilist
    }

    public enum LuceneOperator
    {
        AND,
        OR,
        NOT
    }

    public enum CompareOperator
    {
        GREATERTHAN = 1,
        EQUAL = 2,
        SMALLERTHAN = 3
    }
}