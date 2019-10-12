using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class AncestorQuery : IQueryExpression
    {
        private readonly ContentReference ancestor;
        public AncestorQuery(ContentReference contentLink)
        {
            ancestor = contentLink;
        }
        public string GetExpression()
        {
            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
            IContent content;
            if (contentRepo.TryGet(ancestor, out content))
            {
                return new FieldQuery(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH, "*" + content.ContentLink.ID.ToString(), true).GetExpression();
            }
            return string.Empty;
        }

        public string[] GetFieldName()
        {
            return new string[] { ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH) };
        }
    }
}