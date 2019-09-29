using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using EPiServer.DynamicLuceneExtensions.Attributes;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Extensions;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Repositories
{
    public interface IContentIndexRepository
    {
        ContentIndexResult IndexContent(IContent content, bool includeChild = false);
        SearchDocument GetDocFromContent(IContent content);
        void IncludeDefaultField(Document document, IContent content);
        void RemoveContentIndex(IContent content, bool includeChild = false);
        void RemoveContentLanguageBranch(IContent content);
        ContentIndexResult ReindexSite(IContent siteRoot);
        ContentIndexResult ReindexSiteForRecovery(IContent siteRoot);

        void ResetIndexDirectory();
        long GetIndexFolderSize();
    }

    [ServiceConfiguration(typeof(IContentIndexRepository))]
    public class ContentIndexRepository : IContentIndexRepository
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IContentRepository _contentRepository;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(ContentIndexRepository));
        public ContentIndexRepository(IDocumentRepository documentRepository, IContentRepository contentRepository)
        {
            _documentRepository = documentRepository;
            _contentRepository = contentRepository;
        }

        public virtual ContentIndexResult ReindexSite(IContent siteRoot)
        {
            SlimContentReader slimContentReader = new SlimContentReader(this._contentRepository, siteRoot.ContentLink, (c =>
            {
                return true;
            }));
            var listDocument = new List<SearchDocument>();
            while (slimContentReader.Next())
            {
                if (!slimContentReader.Current.ContentLink.CompareToIgnoreWorkID(ContentReference.RootPage))
                {
                    IVersionable current = slimContentReader.Current as IVersionable;
                    if (current == null || current.Status == VersionStatus.Published)
                    {
                        if (LuceneConfiguration.CanIndexContent(slimContentReader.Current))
                        {
                            var document = GetDocFromContent(slimContentReader.Current);
                            if (document != null)
                            {
                                listDocument.Add(document);
                            }
                        }
                    }
                }
            }
            _documentRepository.ReindexSite(listDocument, siteRoot.ContentGuid);
            return new ContentIndexResult();
        }

        public virtual ContentIndexResult IndexContent(IContent content, bool includeChild = false)
        {
            SlimContentReader slimContentReader = new SlimContentReader(this._contentRepository, content.ContentLink, (c =>
            {
                return includeChild;
            }));
            var listDocument = new List<SearchDocument>();
            while (slimContentReader.Next())
            {
                if (!slimContentReader.Current.ContentLink.CompareToIgnoreWorkID(ContentReference.RootPage))
                {
                    IVersionable current = slimContentReader.Current as IVersionable;
                    if (current == null || current.Status == VersionStatus.Published)
                    {
                        if (LuceneConfiguration.CanIndexContent(slimContentReader.Current))
                        {
                            var document = GetDocFromContent(slimContentReader.Current);
                            if (document != null)
                            {
                                listDocument.Add(document);
                            }
                        }
                    }
                }
            }
            _documentRepository.UpdateBatchIndex(listDocument);
            return new ContentIndexResult();
        }

        public virtual void RemoveContentIndex(IContent content, bool includeChild = false)
        {
            if (!ContentReference.IsNullOrEmpty(content?.ContentLink))
            {
                SlimContentReader slimContentReader = new SlimContentReader(this._contentRepository, content.ContentLink, (c =>
                {
                    return includeChild;
                }));
                var deletedList = new List<string>();
                while (slimContentReader.Next())
                {
                    if (!ContentReference.IsNullOrEmpty(slimContentReader.Current?.ContentLink))
                    {
                        if (!slimContentReader.Current.ContentLink.CompareToIgnoreWorkID(ContentReference.RootPage))
                        {
                            var allLanguageBranch = _contentRepository.GetLanguageBranches<IContent>(slimContentReader.Current.ContentLink);
                            foreach (var lang in allLanguageBranch)
                            {
                                deletedList.Add(SearchDocument.FormatDocumentId(lang));
                            }

                        }
                    }
                    _documentRepository.DeleteFromIndex(deletedList);
                }
            }
        }

        public virtual void RemoveContentLanguageBranch(IContent content)
        {
            var deletedList = new List<string>();
            deletedList.Add(SearchDocument.FormatDocumentId(content));
            _documentRepository.DeleteFromIndex(deletedList);
        }

        public virtual SearchDocument GetDocFromContent(IContent content)
        {
            try
            {
                var document = new Document();
                IncludeDefaultField(document, content);
                var possibleIndexModels = LuceneConfiguration.IncludedTypes.Where(x => x.Value.ContentType.IsAssignableFrom(content.GetOriginalType())).Select(x => x.Value).ToList();
                foreach (var documentIndexModel in possibleIndexModels)
                {
                    IncludeContentField(document, content, documentIndexModel);
                    var computedFieldList = documentIndexModel.IndexedFields;
                    foreach (var field in computedFieldList)
                    {
                        var fieldInstance = field.Value;
                        var value = fieldInstance.GetValue(content, field.Key);
                        if (value != null)
                        {
                            AbstractField luceneField;
                            var indexFieldName = ContentIndexHelpers.GetIndexFieldName(field.Key);
                            var existedFieldName = document.GetField(indexFieldName);
                            if (existedFieldName != null)
                            {
                                document.RemoveField(indexFieldName);
                            }
                            if (fieldInstance.DataType == LuceneFieldType.Multilist)
                            {
                                var listValue = (List<string>)value;
                                foreach (var item in listValue)
                                {
                                    document.Add(new Field(indexFieldName, item, fieldInstance.Store, fieldInstance.Index, fieldInstance.Vector));
                                }
                                continue;
                            }
                            switch (fieldInstance.DataType)
                            {
                                case LuceneFieldType.Datetime:
                                    DateTime d1 = Convert.ToDateTime(value);
                                    luceneField = new Field(indexFieldName, DateTools.DateToString(d1.ToUniversalTime(), DateTools.Resolution.SECOND), fieldInstance.Store, fieldInstance.Index, fieldInstance.Vector);
                                    break;
                                case LuceneFieldType.Numeric:
                                    luceneField = new NumericField(indexFieldName, fieldInstance.Store, true).SetLongValue(string.IsNullOrEmpty(value + "") ? 0 : long.Parse(value + ""));
                                    break;
                                default:
                                    luceneField = new Field(indexFieldName, value.ToString(), fieldInstance.Store, fieldInstance.Index, fieldInstance.Vector);
                                    break;
                            }
                            document.Add(luceneField);
                        }
                    }
                }
                var result = new SearchDocument()
                {
                    Id = SearchDocument.FormatDocumentId(content),
                    Document = document
                };
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Fetch Document error", ex);
                return null;
            }
        }

        public virtual void IncludeDefaultField(Document document, IContent content)
        {
            var versionable = (content as IVersionable);
            var changeTrackable = content as IChangeTrackable;
            var contentStatus = ((int)versionable.Status).ToString();
            var virtualPath = ContentIndexHelpers.GetContentVirtualpath(content);
            var acl = ContentIndexHelpers.GetContentACL(content);
            var contentType = ContentIndexHelpers.GetContentType(content);
            var language = (content as ILocalizable).Language.Name;
            var startPublishDate = versionable.StartPublish.HasValue ? DateTools.DateToString(versionable.StartPublish.Value.ToUniversalTime(), DateTools.Resolution.SECOND) : string.Empty;
            var stopPublishDate = versionable.StopPublish.HasValue ? DateTools.DateToString(versionable.StopPublish.Value.ToUniversalTime(), DateTools.Resolution.SECOND) : string.Empty;
            var expired = "";
            if ((versionable.StopPublish.HasValue && versionable.StopPublish.Value.ToUniversalTime() < DateTime.UtcNow)
                || (versionable.StartPublish.HasValue && versionable.StartPublish.Value.ToUniversalTime() > DateTime.UtcNow))
            {
                expired = "true";
            }
            var createdDate = DateTools.DateToString(changeTrackable.Created.ToUniversalTime(), DateTools.Resolution.SECOND);
            var updatedDate = DateTools.DateToString(changeTrackable.Changed.ToUniversalTime(), DateTools.Resolution.SECOND);
            var idField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ID),
                SearchDocument.FormatDocumentId(content), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var nameField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_NAME),
                content.Name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var statusField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_STATUS),
                contentStatus, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var virtualPathField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH),
                virtualPath, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var aclField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ACL),
                acl, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var typeField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_TYPE),
                contentType, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var langField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_LANGUAGE),
                language, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var startPublishField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_START_PUBLISHDATE),
                startPublishDate, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var stopPublishField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_STOP_PUBLISHDATE),
                stopPublishDate, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var referenceField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_REFERENCE),
                content.ContentLink.ID.ToString(), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var createdField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_CREATED),
                createdDate, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var updatedField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_CHANGED),
                updatedDate, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);
            var expiredField = new Field(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_EXPIRED),
                expired, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES);

            document.Add(idField);
            document.Add(nameField);
            document.Add(statusField);
            document.Add(virtualPathField);
            document.Add(aclField);
            document.Add(typeField);
            document.Add(langField);
            document.Add(startPublishField);
            document.Add(stopPublishField);
            document.Add(referenceField);
            document.Add(createdField);
            document.Add(updatedField);
            document.Add(expiredField);

        }

        public virtual void IncludeContentField(Document document, IContent content, ContentTypeDocument indexModel)
        {
            if (!indexModel.IndexAllFields) return;
            var baseClass = content.GetOriginalType();
            var properties = baseClass.GetProperties().Where(x => x.GetCustomAttributes(typeof(ExcludeFromIndexAttribute), true).Count() == 0
            && x.GetIndexParameters().Length == 0).ToList();
            foreach (var property in properties)
            {
                var field = GetFieldFromProperty(property, content);
                if (field != null)
                {
                    if (document.GetField(field.Name) != null)
                        document.RemoveField(field.Name);
                    document.Add(field);
                }
            }
        }

        private AbstractField GetFieldFromProperty(PropertyInfo property, IContent content)
        {
            AbstractField field = null;
            if (!property.IsPrimitive()) return null;
            var propertyType = property.PropertyType;
            var indexFieldName = GetIndexFieldName(property); var value = property.GetValue(content, null);
            if (value != null)
            {
                if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    DateTime d1 = Convert.ToDateTime(value);
                    field = new Field(indexFieldName, DateTools.DateToString(d1.ToUniversalTime(), DateTools.Resolution.SECOND), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
                }
                else if (propertyType == typeof(bool) || propertyType == typeof(bool?) || propertyType == typeof(Boolean) || propertyType == typeof(Boolean?))
                {
                    if (bool.TryParse(value + "", out var tmp))
                    {
                        field = new Field(indexFieldName, tmp.ToString().ToLower(), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
                    }
                }
                else if (property.IsNumeric())
                {
                    if (long.TryParse(value + "", out var tmp))
                    {
                        field = new NumericField(indexFieldName, Field.Store.YES, true).SetLongValue(tmp);
                    }
                }
                else
                {
                    field = new Field(indexFieldName, value.ToString(), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
                }
            }
            return field;
        }

        private string GetIndexFieldName(PropertyInfo property)
        {
            var fieldName = ((IndexFieldNameAttribute)property.GetCustomAttributes(typeof(IndexFieldNameAttribute), true).FirstOrDefault())?.IndexFieldName
                    ?? property.Name;
            return ContentIndexHelpers.GetIndexFieldName(fieldName);
        }

        public virtual void ResetIndexDirectory()
        {
            try
            {
                string folderPath = ConfigurationManager.AppSettings["lucene:BlobConnectionString"];
                var serverPath = HttpRuntime.AppDomainAppPath;
                folderPath = serverPath + folderPath;
                var fsDirectory = FSDirectory.Open(folderPath);
                if (IndexWriter.IsLocked(fsDirectory))
                {
                    IndexWriter.Unlock(fsDirectory);
                }
                string[] files = System.IO.Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
                if (!DirectoryReader.IndexExists(fsDirectory))
                {
                    using (new IndexWriter(fsDirectory, new StandardAnalyzer(LuceneConfiguration.LuceneVersion), true, IndexWriter.MaxFieldLength.UNLIMITED))
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("ResetIndexDirectory error", ex);
            }
        }

        public virtual long GetIndexFolderSize()
        {
            string folderPath = ConfigurationManager.AppSettings["lucene:BlobConnectionString"];
            var serverPath = HttpRuntime.AppDomainAppPath;
            folderPath = serverPath + folderPath;
            if (System.IO.Directory.Exists(folderPath))
            {
                DirectoryInfo dir = new DirectoryInfo(folderPath);
                dir.Refresh();
                long size = 0;
                string[] files = System.IO.Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    if (!File.Exists(file)) continue;
                    FileInfo details = new FileInfo(file);
                    if (details != null)
                    {
                        details.Refresh();
                        size += details.Length;
                    }
                }
                return size;
            }
            return -1;
        }

        public ContentIndexResult ReindexSiteForRecovery(IContent siteRoot)
        {
            SlimContentReader slimContentReader = new SlimContentReader(this._contentRepository, siteRoot.ContentLink, (c =>
            {
                return true;
            }));
            var listDocument = new List<SearchDocument>();
            while (slimContentReader.Next())
            {
                if (!slimContentReader.Current.ContentLink.CompareToIgnoreWorkID(ContentReference.RootPage))
                {
                    IVersionable current = slimContentReader.Current as IVersionable;
                    if (current == null || current.Status == VersionStatus.Published)
                    {
                        if (LuceneConfiguration.CanIndexContent(slimContentReader.Current))
                        {
                            var document = GetDocFromContent(slimContentReader.Current);
                            if (document != null)
                            {
                                listDocument.Add(document);
                            }
                        }
                    }
                }
            }
            _documentRepository.ReindexSiteForRecovery(listDocument, siteRoot.ContentGuid);
            return new ContentIndexResult();
        }
    }
}