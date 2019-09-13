using Lucene.Net.Documents;
using System;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class DateRangeQuery : RangeQuery
    {
        public DateRangeQuery(DateTime startDate, DateTime endDate, string fieldName, bool inclusive)
            : base(DateTools.DateToString(startDate, DateTools.Resolution.SECOND), DateTools.DateToString(endDate, DateTools.Resolution.SECOND), fieldName, inclusive)
        {

        }
    }
}