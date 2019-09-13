using EPiServer;
using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EPiServer.DynamicLuceneExtensions.Helpers
{
    public class SlimContentReader
    {
        private Stack<ContentReference> _backlog = new Stack<ContentReference>();
        private Queue<IContent> _queue = new Queue<IContent>();
        private IContentRepository _contentRepository;
        private Func<IContent, bool> _traverseChildren;

        public SlimContentReader(IContentRepository contentRepository, ContentReference start)
          : this(contentRepository, start, (Func<IContent, bool>)(c => true))
        {
        }

        public SlimContentReader(
          IContentRepository contentRepository,
          ContentReference start,
          Func<IContent, bool> traverseChildren)
        {
            this._contentRepository = contentRepository;
            this._backlog.Push(start);
            this._traverseChildren = traverseChildren;
        }

        public IContent Current { get; private set; }

        public bool Next()
        {
            if (this._backlog.Count == 0 && this._queue.Count == 0)
                return false;
            if (this._queue.Count == 0)
            {
                bool flag = true;
                ContentReference contentLink = this._backlog.Pop();
                foreach (IContent languageBranch in this._contentRepository.GetLanguageBranches<IContent>(contentLink))
                {
                    flag &= this._traverseChildren(languageBranch);
                    this._queue.Enqueue(languageBranch);
                }
                if (flag)
                {
                    IContent[] array = this._contentRepository.GetChildren<IContent>(contentLink, CultureInfo.InvariantCulture).ToArray<IContent>();
                    for (int length = array.Length; length > 0; --length)
                        this._backlog.Push(new ContentReference(array[length - 1].ContentLink.ID, array[length - 1].ContentLink.ProviderName));
                }
            }
            this.Current = this._queue.Dequeue();
            return true;
        }
    }
}