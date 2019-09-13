using EPiServer.Core;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System.Collections.ObjectModel;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class VirtualPathQuery : IQueryExpression
    {
        private Collection<string> _virtualPathNodes = new Collection<string>();

        public Collection<string> VirtualPathNodes
        {
            get
            {
                return this._virtualPathNodes;
            }
        }

        public string[] GetFieldName()
        {
            return new string[] { ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH) };
        }

        public virtual string GetExpression()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH) + ":(");
            foreach (string virtualPathNode in this.VirtualPathNodes)
            {
                stringBuilder.Append(LuceneQueryHelper.Escape(virtualPathNode.Replace(" ", "")));
                stringBuilder.Append("|");
            }
            if (stringBuilder.Length > 0)
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            stringBuilder.Append("*)");
            return stringBuilder.ToString();
        }


        public void AddContentNodes(ContentReference contentLink)
        {
            if (ContentReference.IsNullOrEmpty(contentLink))
                return;
            foreach (string virtualPathNode in ContentIndexHelpers.GetVirtualPathNodes(contentLink))
                _virtualPathNodes.Add(virtualPathNode);
        }


    }
}