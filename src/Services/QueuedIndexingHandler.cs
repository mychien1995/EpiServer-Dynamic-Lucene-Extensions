using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using EPiServer.Events;
using EPiServer.Events.Clients;
using EPiServer.Logging;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;

namespace EPiServer.DynamicLuceneExtensions.Services
{
    public class QueuedIndexingHandler : IIndexingHandler
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(QueuedIndexingHandler));
        private static ConcurrentQueue<IndexRequestItem> _requestQueue = new ConcurrentQueue<IndexRequestItem>();
        private static Timer _queueProcessTimer;
        private static int BatchSize = 100;
        private static int MaximumQueueSize = 50000;
        public static readonly Guid LocalRaiserId = Guid.NewGuid();
        public static readonly Guid IndexContentEventId = new Guid("c3dbffe3-81b4-4a80-9c7f-8ec91a8397ec");
        private readonly IEventRegistry _eventService;

        private readonly double _timerInterval = 20000;
        private readonly IIndexingHandler _localIndexingHandler;
        private static object _lock = new object();
        public QueuedIndexingHandler(IEventRegistry eventRegistry)
        {
            _localIndexingHandler = new IndexingHandler();
            _eventService = eventRegistry;
        }
        private void IndexContent_Raised(object sender, EventNotificationEventArgs e)
        {
            if (e.RaiserId == LocalRaiserId)
                return;
            this.ProcessRequestInternal(IndexRequestItem.Parse((string)e.Param));
        }
        public void ProcessRequestInternal(IndexRequestItem request)
        {
            if (request == null || !IsAvailable()) return;
            _requestQueue.Enqueue(request);
            _queueProcessTimer.Enabled = true;
        }

        public void ProcessRequest(IndexRequestItem request)
        {
            if (request == null || !IsAvailable()) return;
            _requestQueue.Enqueue(request);
            this._eventService.Get(IndexContentEventId).RaiseAsync(LocalRaiserId, request.RemoteRequest, EventRaiseOption.RaiseBroadcast);
            _queueProcessTimer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                try
                {
                    int count = 0;
                    var processItems = new List<IndexRequestItem>();
                    while (count < BatchSize)
                    {
                        if (_requestQueue.Count == 0) break;
                        count++;
                        IndexRequestItem item;
                        if (_requestQueue.TryDequeue(out item))
                        {
                            if (item != null)
                            {
                                processItems.Add(item);
                            }
                        }
                    }
                    if (LuceneContext.AllowIndexing)
                        _localIndexingHandler.ProcessRequests(processItems);
                }
                catch (Exception ex)
                {
                    _logger.Error("Lucene Queue Process error", ex);
                }
                finally
                {
                    if (_requestQueue.Count > 0)
                        _queueProcessTimer.Enabled = true;
                }
            }
        }
        public int GetQueueSize()
        {
            return _requestQueue.Count;
        }
        public bool IsAvailable()
        {
            if (_queueProcessTimer == null)
            {
                lock (_lock)
                {
                    if (_queueProcessTimer == null)
                    {
                        _queueProcessTimer = new Timer(this._timerInterval);
                        _queueProcessTimer.AutoReset = false;
                        _queueProcessTimer.Elapsed += new ElapsedEventHandler(this.Timer_Elapsed);
                    }
                }
            }
            //stop serving if queue is full
            if (_requestQueue.Count > MaximumQueueSize) return false;
            return true;
        }
        public void ProcessRequests(IEnumerable<IndexRequestItem> requests)
        {

        }

        public void Init()
        {
            Event @event = this._eventService.Get(IndexContentEventId);
            @event.Raised += new EventNotificationHandler(this.IndexContent_Raised);
        }
    }
}