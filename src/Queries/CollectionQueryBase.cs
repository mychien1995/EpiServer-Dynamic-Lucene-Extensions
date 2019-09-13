using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class CollectionQueryBase : IQueryExpression
    {
        private Collection<string> _items = new Collection<string>();

        protected CollectionQueryBase(string itemFieldName, LuceneOperator innerOperator)
        {
            this.InnerOperator = innerOperator;
            this.IndexFieldName = itemFieldName;
        }

        public LuceneOperator InnerOperator { get; private set; }

        public string IndexFieldName { get; private set; }

        public Collection<string> Items
        {
            get
            {
                return this._items;
            }
        }

        public virtual string GetExpression()
        {
            StringBuilder stringBuilder = new StringBuilder();
            Collection<string> collection = CollectionQueryBase.RemoveDuplicates(this.Items);
            int num = 0;
            foreach (string str in collection)
            {
                ++num;
                stringBuilder.Append(this.IndexFieldName + ":(");
                if (num < collection.Count)
                {
                    stringBuilder.Append(LuceneQueryHelper.Escape(str));
                    stringBuilder.Append(") ");
                    stringBuilder.Append(Enum.GetName(typeof(LuceneOperator), (object)this.InnerOperator));
                    stringBuilder.Append(" ");
                }
                else
                {
                    stringBuilder.Append(LuceneQueryHelper.Escape(str));
                    stringBuilder.Append(")");
                }
            }
            return stringBuilder.ToString();
        }

        private static Collection<string> RemoveDuplicates(Collection<string> inputList)
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            Collection<string> collection = new Collection<string>();
            foreach (string input in inputList)
            {
                if (!dictionary.ContainsKey(input))
                {
                    dictionary.Add(input, 0);
                    collection.Add(input);
                }
            }
            return collection;
        }

        public string[] GetFieldName()
        {
            return new string[] { IndexFieldName };
        }
    }
}