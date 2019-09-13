using EPiServer;
using EPiServer.PlugIn;
using EPiServer.Web;
using EPiServer.DynamicLuceneExtensions.Business.ScheduledJobRunners;
using EPiServer.DynamicLuceneExtensions.Repositories;
using System.Configuration;
using System.Linq;
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
        private readonly IDocumentRepository _documentRepository;
        private readonly IRemoteContentIndexRepository _remoteContentIndexRepository;
        public LuceneSiteIndexController(ISiteDefinitionRepository siteDefinitionRepository, IContentIndexRepository contentIndexRepository
            , IContentRepository contentRepository, IDocumentRepository documentRepository, IRemoteContentIndexRepository remoteContentIndexRepository)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _contentIndexRepository = contentIndexRepository;
            _contentRepository = contentRepository;
            _documentRepository = documentRepository;
            _remoteContentIndexRepository = remoteContentIndexRepository;
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
            //var siteContent = _contentRepository.Get<IContent>(new ContentReference(idList.First()));
            //_contentIndexRepository.IndexContent(siteContent, true);
            new LuceneSiteIndexJobRunner().RunIndexing(idList);
            ViewBag.Message = "The Job is running";
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult ResetIndex()
        {
            _remoteContentIndexRepository.ResetIndexDirectory(Server.MapPath("~") + ConfigurationManager.AppSettings["lucene:BlobConnectionString"]);
            ViewBag.Message = "Index folder reseted";
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }

        [HttpGet]
        public ActionResult GetIndexSize()
        {
            var size = _documentRepository.GetIndexFolderSize(Server.MapPath("~") + ConfigurationManager.AppSettings["lucene:BlobConnectionString"]);
            ViewBag.Message = "Index folder size " + size;
            return View("~/Views/admin/LuceneSiteIndex/Index.cshtml");
        }
    }
}