using EPiServer;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.DynamicLuceneExtensions.Configurations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Helpers
{
    public static class ContentIndexHelpers
    {
        public static string GetContentVirtualpath(IContent content)
        {
            return string.Join("|", GetVirtualPathNodes(content));
        }

        public static string GetContentVirtualpath(ContentReference contentLink)
        {
            return string.Join("|", GetVirtualPathNodes(contentLink));
        }

        public static string GetIndexFieldId(ContentReference contentLink)
        {
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            var content = _contentRepository.Get<IContent>(contentLink);
            return content.ContentGuid.ToString();
        }

        public static ICollection<string> GetVirtualPathNodes(IContent content)
        {
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            Collection<string> collection = new Collection<string>();
            foreach (IContent parent in _contentRepository.GetAncestors(content.ContentLink).Reverse())
                collection.Add(parent.ContentGuid.ToString());
            collection.Add(content.ContentGuid.ToString());
            return collection;
        }

        public static ICollection<string> GetVirtualPathNodes(ContentReference contentLink)
        {
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            Collection<string> collection = new Collection<string>();
            foreach (IContent content in _contentRepository.GetAncestors(contentLink).Reverse<IContent>())
                collection.Add(content.ContentGuid.ToString());
            IContent content1 = _contentRepository.Get<IContent>(contentLink);
            collection.Add(content1.ContentGuid.ToString());
            return (ICollection<string>)collection;
        }

        public static string GetContentACL(IContent content)
        {
            var accessControlList = new List<string>();
            IContentSecurable contentSecurable = content as IContentSecurable;
            IContentSecurityDescriptor securityDescriptor = contentSecurable.GetContentSecurityDescriptor();
            foreach (AccessControlEntry accessControlEntry in securityDescriptor.Entries)
            {
                if ((accessControlEntry.Access & AccessLevel.Read) == AccessLevel.Read)
                    accessControlList.Add(string.Format("{0}:{1}", accessControlEntry.EntityType == SecurityEntityType.User ? "U" : "G", accessControlEntry.Name));
            }
            return string.Join("|", accessControlList);
        }

        public static string GetContentType(IContent content)
        {
            var contentType = content.GetOriginalType();
            StringBuilder stringBuilder = new StringBuilder(contentType.FullName);
            if (contentType.IsClass)
            {
                while (contentType.BaseType != typeof(object))
                {
                    contentType = contentType.BaseType;
                    stringBuilder.Append("|");
                    stringBuilder.Append(contentType.FullName);
                }
            }
            stringBuilder.Append("|");
            stringBuilder.Append("EPiServer.Core.IContent");
            return stringBuilder.ToString();
        }

        public static string GetIndexFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(LuceneConfiguration.Prefix)) return fieldName;
            return LuceneConfiguration.Prefix + "_" + fieldName;
        }


    }
}