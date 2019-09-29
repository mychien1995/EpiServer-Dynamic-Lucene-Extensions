using EPiServer;
using EPiServer.Core;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.WindowsAzure.Storage;
using EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Indexing;
using EPiServer.DynamicLuceneExtensions.Indexing.ComputedFields;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;
namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class LuceneConfiguration
    {
        private static object _lock = new object();
        private static bool _initialized;

        private static Dictionary<string, ContentTypeDocument> _includedTypes;
        private static bool? _isActive;
        private static string _fieldPrefix;
        private static bool? _indexAllTypes;
        private static Directory _directory;
        private static string _luceneVersion;
        private static Analyzer _analyzer;

        public static bool Active
        {
            get
            {
                if (_isActive == null)
                {
                    Initialize();
                }
                return _isActive.Value;
            }
        }

        public static Analyzer Analyzer
        {
            get
            {
                if (_analyzer == null)
                {
                    Initialize();
                }
                return _analyzer;
            }
        }
        public static Version LuceneVersion
        {
            get
            {
                if (_luceneVersion == null)
                {
                    Initialize();
                }
                return (Version)Enum.Parse(typeof(Version), _luceneVersion);
            }
        }

        public static Dictionary<string, ContentTypeDocument> IncludedTypes
        {
            get
            {
                if (_includedTypes == null)
                {
                    Initialize();
                }
                return _includedTypes;
            }
        }

        public static bool IndexAllTypes
        {
            get
            {
                if (_indexAllTypes == null)
                {
                    Initialize();
                }
                return _indexAllTypes.Value;
            }
        }

        public static Directory Directory
        {
            get
            {
                if (_directory == null)
                {
                    Initialize();
                }
                return _directory;
            }
        }

        public static string Prefix
        {
            get
            {
                if (_fieldPrefix == null)
                {
                    Initialize();
                }
                return _fieldPrefix;
            }
        }

        public static bool CanIndexContent(IContent content)
        {
            if (content == null) return false;
            if (IndexAllTypes) return true;
            return IncludedTypes.Any(c => c.Value.ContentType.IsAssignableFrom(content.GetOriginalType()));
        }

        private static void Initialize()
        {
            if (_initialized)
                return;
            lock (_lock)
            {
                if (_initialized)
                    return;
                try
                {
                    LuceneSection section = (LuceneSection)ConfigurationManager.GetSection("episerver.lucene.indexing");
                    _indexAllTypes = section.IndexAllTypes;
                    _isActive = section.Active;
                    if (_isActive == null || !_isActive.Value)
                    {
                        _initialized = true;
                        return;
                    }
                    _fieldPrefix = section.Prefix;
                    _luceneVersion = section.LuceneVersion;
                    _includedTypes = new Dictionary<string, ContentTypeDocument>();
                    string[] strArray = new string[0];
                    var fieldAnalyzerWrapper = new PerFieldAnalyzerWrapper((Analyzer)new StandardAnalyzer(LuceneVersion, StopFilter.MakeStopSet(strArray)));
                    if (!IndexAllTypes)
                    {
                        foreach (IncludedTypeElement typeSetting in section.IncludedTypes)
                        {
                            Type contentType = Type.GetType(typeSetting.Type, true, true);
                            if (contentType == null) continue;
                            if (!_includedTypes.TryGetValue(typeSetting.Name, out var tmp))
                            {
                                var documentIndexModel = new ContentTypeDocument();
                                documentIndexModel.ContentType = contentType;
                                documentIndexModel.IndexAllFields = typeSetting.IndexAllFields;
                                _includedTypes.Add(typeSetting.Name, documentIndexModel);
                                foreach (IncludedFieldElement fieldSetting in typeSetting.IncludedFields)
                                {
                                    Type fieldType;
                                    if (string.IsNullOrEmpty(fieldSetting.Type))
                                    {
                                        fieldType = typeof(DefaultComputedField);
                                    }
                                    else fieldType = Type.GetType(fieldSetting.Type, true, true);
                                    if (!typeof(IComputedField).IsAssignableFrom(fieldType)) continue;
                                    var instance = (IComputedField)Activator.CreateInstance(fieldType);
                                    Type analyzerType = Type.GetType(fieldSetting.Analyzer, true, true);
                                    if (!typeof(Analyzer).IsAssignableFrom(analyzerType)) continue;
                                    if (analyzerType == typeof(StandardAnalyzer))
                                    {
                                        instance.Analyzer = new StandardAnalyzer(LuceneVersion, StopFilter.MakeStopSet(strArray));
                                    }
                                    else
                                    {
                                        instance.Analyzer = (Analyzer)Activator.CreateInstance(analyzerType);
                                    }
                                    instance.Index = fieldSetting.Index;
                                    instance.Store = fieldSetting.Store;
                                    instance.Vector = fieldSetting.Vector;
                                    instance.DataType = fieldSetting.DataType;
                                    if (!documentIndexModel.IndexedFields.TryGetValue(fieldSetting.Name, out var tmp2))
                                    {
                                        documentIndexModel.IndexedFields.Add(fieldSetting.Name, instance);
                                        fieldAnalyzerWrapper.AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(fieldSetting.Name), instance.Analyzer);
                                    }
                                }
                            }
                        }
                    }
                    _analyzer = fieldAnalyzerWrapper;
                    AddDefaultFieldAnalyzer();
                    var directoryConnectionString = ConfigurationManager.AppSettings["lucene:BlobConnectionString"] ?? "App_Data/My_Index";
                    var directoryContainerName = ConfigurationManager.AppSettings["lucene:ContainerName"] ?? "lucene";
                    var directoryType = (ConfigurationManager.AppSettings["lucene:DirectoryType"] ?? "Filesystem").ToLower();
                    switch (directoryType)
                    {
                        case Constants.ContainerType.Azure:
                            var connectionString = directoryConnectionString;
                            var containerName = directoryContainerName;
                            var storageAccount = CloudStorageAccount.Parse(connectionString);
                            var azureDir = new FastAzureDirectory(storageAccount, containerName, new RAMDirectory());
                            _directory = azureDir;
                            break;
                        default:
                            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directoryConnectionString);
                            var fsDirectory = FSDirectory.Open(folderPath);
                            _directory = fsDirectory;
                            break;
                    }
                    InitDirectory(_directory);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                _initialized = true;
            }
        }

        private static void InitDirectory(Directory directory)
        {
            if (!DirectoryReader.IndexExists(directory))
            {
                using (new IndexWriter(_directory, new StandardAnalyzer(LuceneVersion), true, IndexWriter.MaxFieldLength.UNLIMITED))
                {

                }
            }
        }

        public static void AddAnalyzer(string fieldName, Analyzer analyzer)
        {
            if (_analyzer == null) return;
            var currentAnalyzer = (PerFieldAnalyzerWrapper)_analyzer;
            currentAnalyzer.AddAnalyzer(fieldName, analyzer);
        }

        public static void AddDefaultFieldAnalyzer()
        {
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ID), new KeywordAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_NAME), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_STATUS), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_VIRTUAL_PATH), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ACL), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_TYPE), new StandardAnalyzer(LuceneVersion));
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_LANGUAGE), new KeywordAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_START_PUBLISHDATE), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_STOP_PUBLISHDATE), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_REFERENCE), new KeywordAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_CREATED), new WhitespaceAnalyzer());
            AddAnalyzer(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_CHANGED), new WhitespaceAnalyzer());
        }
    }
}