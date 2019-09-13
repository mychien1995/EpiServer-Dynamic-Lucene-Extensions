using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class RangeQuery : IQueryExpression
    {
        public RangeQuery(string start, string end, string fieldName, bool inclusive)
        {
            this.Start = start;
            this.End = end;
            this.Field = fieldName;
            this.Inclusive = inclusive;
        }

        public string Start { get; set; }

        public string End { get; set; }

        public string Field { get; set; }

        public bool Inclusive { get; set; }

        public string[] GetFieldName()
        {
            return new string[] { ContentIndexHelpers.GetIndexFieldName(Field) };
        }

        public string GetExpression()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ContentIndexHelpers.GetIndexFieldName(Field));
            stringBuilder.Append(":");
            stringBuilder.Append(this.Inclusive ? "[" : "{");
            stringBuilder.Append(LuceneQueryHelper.Escape(this.Start));
            stringBuilder.Append(" TO ");
            stringBuilder.Append(LuceneQueryHelper.Escape(this.End));
            stringBuilder.Append(this.Inclusive ? "]" : "}");
            return stringBuilder.ToString();
        }
    }
}