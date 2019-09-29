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
        public const string CALCULATESIZE = "calculate";
        public const string RECOVERINDEX = "recover";

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
            if (sections.Length > 0 && sections[0] == RESETINDEX)
            {
                if (sections.Length == 1)
                    return new ResetIndexRequestItem();
                else
                {
                    var machineId = Guid.Parse(sections[1]);
                    return new ResetIndexRequestItem(machineId);
                }
            }
            if (sections.Length > 0 && sections[0] == RECOVERINDEX)
            {
                if (sections.Length == 1)
                    return new RecoverIndexRequestItem();
                else
                {
                    var machineId = Guid.Parse(sections[1]);
                    return new RecoverIndexRequestItem(machineId);
                }
            }
            if (sections.Length > 0 && sections[0] == CALCULATESIZE)
            {
                return new GetIndexSizeRequestItem();
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
            var actionList = new List<string>() { REINDEX, REMOVE, REMOVE_LANGUAGE, REINDEXSITE, RESETINDEX, RECOVERINDEX, CALCULATESIZE };
            if (!actionList.Contains(action)) return null;
            if (!bool.TryParse(sections[2], out includeChild)) return null;
            return new IndexRequestItem(content, action, includeChild);

        }

    }

    public class ResetIndexRequestItem : IndexRequestItem
    {
        public Guid? TargetMachine { get; set; }
        public ResetIndexRequestItem()
        {
            Action = RESETINDEX;
        }
        public ResetIndexRequestItem(Guid? targetMachine)
        {
            Action = RESETINDEX;
            TargetMachine = targetMachine;
        }
        public override string RemoteRequest
        {
            get
            {
                if (TargetMachine != Guid.Empty && TargetMachine != null) return Action + "|" + TargetMachine;
                return Action;
            }
        }
    }

    public class GetIndexSizeRequestItem : IndexRequestItem
    {
        public GetIndexSizeRequestItem()
        {
            Action = CALCULATESIZE;
        }
        public override string RemoteRequest => Action;
    }

    public class RecoverIndexRequestItem : IndexRequestItem
    {
        public Guid? TargetMachine { get; set; }
        public RecoverIndexRequestItem()
        {
            Action = RECOVERINDEX;
        }
        public RecoverIndexRequestItem(Guid? targetMachine)
        {
            Action = RECOVERINDEX;
            TargetMachine = targetMachine;
        }
        public override string RemoteRequest
        {
            get
            {
                if (TargetMachine != Guid.Empty && TargetMachine != null) return Action + "|" + TargetMachine;
                return Action;
            }
        }
    }

}