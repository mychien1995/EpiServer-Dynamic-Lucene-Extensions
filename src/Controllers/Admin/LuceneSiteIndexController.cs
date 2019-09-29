using EPiServer;
using EPiServer.Data.Dynamic;
using EPiServer.PlugIn;
using EPiServer.Web;
using EPiServer.DynamicLuceneExtensions.Business.ScheduledJobRunners;
using EPiServer.DynamicLuceneExtensions.Models;
using EPiServer.DynamicLuceneExtensions.Repositories;
using EPiServer.DynamicLuceneExtensions.Services;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace EPiServer.DynamicLuceneExtensions.Controllers.Admin
{
    [GuiPlugIn(
        Area = PlugInArea.AdminMenu,
        Url = "/cms/admin/LuceneSiteIndex/Index",
        DisplayName = "Lucene Site Content Index Runner")]
    public class LuceneSiteIndexController : Controller
    {
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly IContentIndexRepository _contentIndexRepository;
        private readonly IContentRepository _contentRepository;
        private readonly IRemoteContentIndexRepository _remoteContentIndexRepository;
        private readonly IIndexingHandler _indexingHandler;
        public LuceneSiteIndexController(ISiteDefinitionRepository siteDefinitionRepository, IContentIndexRepository contentIndexRepository
            , IContentRepository contentRepository, IRemoteContentIndexRepository remoteContentIndexRepository
            , IIndexingHandler indexingHandler)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _contentIndexRepository = contentIndexRepository;
            _contentRepository = contentRepository;
            _remoteContentIndexRepository = remoteContentIndexRepository;
            _indexingHandler = indexingHandler;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var siteList = _siteDefinitionRepository.List();
            ViewBag.SiteList = siteList;
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpPost]
        public ActionResult Reindex()
        {
            var siteId = Request.Form["siteId"];
            if (string.IsNullOrEmpty(siteId))
            {
                ViewBag.Message = "Please select a site to re-index";
                var siteList = _siteDefinitionRepository.List();
                ViewBag.SiteList = siteList;
                return View("~/Views/Admin/LuceneSiteIndex/Index.cshtml");
            }
            var idList = siteId.Split(',').Select(x => int.Parse(x)).ToList();
            new LuceneSiteIndexJobRunner().RunIndexing(idList);
            ViewBag.Message = "The Job is running";
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult ResetIndex(Guid? targetId = null, string machineName = null)
        {
            _remoteContentIndexRepository.ResetIndexDirectory(targetId);
            if (targetId == null)
            {
                ViewBag.Message = $"Index folder reseted - Requested Server: {Server.MachineName} to all";
            }
            else
            {
                ViewBag.Message = $"Index folder reseted - Requested Server: {Server.MachineName} to server: {machineName}";
            }
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult RecoverIndex(Guid? targetId = null, string machineName = null)
        {
            Task.Run(() =>
            {
                _remoteContentIndexRepository.RecoverIndex(targetId);
            });
            if (targetId == null)
            {
                ViewBag.Message = $"Index folder recovering - Requested Server: {Server.MachineName} to all";
            }
            else
            {
                ViewBag.Message = $"Index folder recovering - Requested Server: {Server.MachineName} to server: {machineName}";
            }
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult CheckQueue()
        {
            if (!(_indexingHandler is QueuedIndexingHandler)) ViewBag.Message = "Currently not using queue";
            else
            {
                var handler = _indexingHandler as QueuedIndexingHandler;
                var machineName = Server.MachineName;
                var itemCount = handler.GetQueueSize();
                ViewBag.Message = "Pending Queue items: " + itemCount + "- Requested Server: " + machineName;
            }
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult CheckStatus()
        {
            DynamicDataStore store = typeof(ServerInfomation).GetOrCreateStore();
            ViewBag.DataTable = store.LoadAll<ServerInfomation>();
            ViewBag.CurrentServer = RemoteContentIndexRepository.LocalRaiserId;
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult GetIndexSize()
        {
            var size = _remoteContentIndexRepository.GetIndexFolderSize();
            var machineName = Server.MachineName;
            ViewBag.Message = "Index folder size " + (size / 1024) + "KB - Requested Server: " + machineName;
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }
    }
}