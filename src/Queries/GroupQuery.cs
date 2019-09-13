using EPiServer.DynamicLuceneExtensions.Models.Search;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class GroupQuery : IQueryExpression
    {
        private Collection<IQueryExpression> _queries = new Collection<IQueryExpression>();

        public GroupQuery(LuceneOperator innerOperator)
        {
            this.InnerOperator = innerOperator;
        }

        public LuceneOperator InnerOperator { get; set; }

        public Collection<IQueryExpression> QueryExpressions
        {
            get
            {
                return this._queries;
            }
        }

        public string[] GetFieldName()
        {
            return _queries.SelectMany(x => x.GetFieldName()).ToArray();
        }

        public string GetExpression()
        {
            StringBuilder stringBuilder = new StringBuilder();
            int num = 0;
            int count = this.QueryExpressions.Count;
            foreach (IQueryExpression queryExpression in this.QueryExpressions)
            {
                ++num;
                if (count > 1)
                    stringBuilder.Append("(");
                stringBuilder.Append(queryExpression.GetExpression());
                if (num < this.QueryExpressions.Count)
                    stringBuilder.Append(") " + Enum.GetName(typeof(LuceneOperator), (object)this.InnerOperator) + " ");
                else if (count > 1)
                    stringBuilder.Append(")");
            }
            return stringBuilder.ToString();
        }
    }
}