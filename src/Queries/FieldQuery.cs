using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class FieldQuery : IQueryExpression
    {
        public FieldQuery(string fieldName, string value, bool useWildCard = false, float? boost = null)
        {
            FieldName = fieldName;
            Value = value;
            UseWildCard = useWildCard;
            Boost = boost;
        }

        public string FieldName { get; set; }
        public string Value { get; set; }
        public bool UseWildCard { get; set; }
        public float? Boost { get; set; }

        public string GetExpression()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ContentIndexHelpers.GetIndexFieldName(FieldName));
            stringBuilder.Append(":(");
            stringBuilder.Append(LuceneQueryHelper.EscapeParenthesis(Value));
            if (UseWildCard)
            {
                stringBuilder.Append("*");
            }
            if (Boost.HasValue)
            {
                stringBuilder.Append($"^{Boost}");
            }
            stringBuilder.Append(")");

            return stringBuilder.ToString();
        }

        public string[] GetFieldName()
        {
            return new string[] { ContentIndexHelpers.GetIndexFieldName(FieldName) };
        }
    }
}