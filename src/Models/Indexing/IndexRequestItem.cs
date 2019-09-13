using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Models.Indexing
{
    public class IndexRequestItem
    {
        public int ContentId { get; set; }
        public IContent Content { get; set; }
        public string Action { get; set; }

        public string Language { get; set; }
        public bool IncludeChild { get; set; }

        public const string REINDEX = "reindex";
        public const string REMOVE = "remove";
        public const string REMOVE_LANGUAGE = "removelang";
        public const string REINDEXSITE = "reindexsite";
        public const string RESETINDEX = "resetindex";

        public virtual string RemoteRequest
        {
            get
            {
                if (Content?.ContentLink == null || string.IsNullOrEmpty(Action)) return string.Empty;
                return $"{Content.ContentLink.ID}|{Action}|{IncludeChild}|{Language}";
            }
        }

        public IndexRequestItem()
        {

        }
        public IndexRequestItem(IContent content, string action = REINDEX, bool includeChild = false)
        {
            this.Content = content;
            if (content?.ContentLink != null)
                this.ContentId = content.ContentLink.ID;
            this.Action = action;
            this.IncludeChild = includeChild;
        }

        public static IndexRequestItem Parse(string remoteFormat)
        {
            if (string.IsNullOrEmpty(remoteFormat)) return null;
            var sections = remoteFormat.Split('|');
            if (sections.Length > 1 && sections[0] == RESETINDEX)
            {
                return new ResetIndexRequestItem(sections[1]);
            }
            if (sections.Length < 3) return null;
            int contentId;
            var action = sections[1];
            bool includeChild;
            if (!int.TryParse(sections[0], out contentId)) return null;
            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
            if (contentRepo == null) return null;
            IContent content;
            if (!contentRepo.TryGet<IContent>(new ContentReference(contentId), out content)) return null;
            var actionList = new List<string>() { REINDEX, REMOVE, REMOVE_LANGUAGE, REINDEXSITE, RESETINDEX };
            if (!actionList.Contains(action)) return null;
            if (!bool.TryParse(sections[2], out includeChild)) return null;
            return new IndexRequestItem(content, action, includeChild);

        }

    }

    public class ResetIndexRequestItem : IndexRequestItem
    {
        public string FolderPath { get; set; }
        public ResetIndexRequestItem(string path)
        {
            FolderPath = path;
            Action = RESETINDEX;
        }
        public override string RemoteRequest => Action + "|" + FolderPath;
    }

}