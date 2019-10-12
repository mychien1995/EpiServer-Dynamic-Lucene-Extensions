using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.WindowsAzure.Storage;
using EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend;
using EPiServer.DynamicLuceneExtensions.Configurations;
using EPiServer.DynamicLuceneExtensions.Models.Indexing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Models
{
    public interface IIndexShardingStrategy
    {
        IndexShard LocateShard(IndexRequestItem indexRequest);
        IndexShard GetOrCreateShard(string shardName);
        void WarmupShards();
    }

    public class SiteBasedShardingStrategy : IIndexShardingStrategy
    {
        private ConcurrentDictionary<string, IndexShard> _shardCollections;
        private IContentRepository _contentRepository;
        private ISiteDefinitionResolver _siteDefinitionResolver;
        public SiteBasedShardingStrategy()
        {
            _shardCollections = new ConcurrentDictionary<string, IndexShard>();
        }

        public IndexShard GetOrCreateShard(string shardName)
        {
            BuildServices();
            var shard = new IndexShard();
            shard.Name = shardName;
            if (_shardCollections.TryGetValue(shard.Name, out shard)) return shard;
            var directoryType = (ConfigurationManager.AppSettings["lucene:DirectoryType"] ?? "Filesystem").ToLower();
            Lucene.Net.Store.Directory directory = null;
            var directoryConnectionString = ConfigurationManager.AppSettings["lucene:BlobConnectionString"] ?? "App_Data/My_Index";
            switch (directoryType)
            {
                case Constants.ContainerType.Azure:
                    var directoryContainerName = ConfigurationManager.AppSettings["lucene:ContainerName"] ?? "lucene";
                    var connectionString = directoryConnectionString;
                    var containerName = directoryContainerName;
                    var storageAccount = CloudStorageAccount.Parse(connectionString);
                    var azureDir = new FastAzureDirectory(storageAccount, containerName, new RAMDirectory(), shardName);
                    directory = azureDir;
                    break;
                case Constants.ContainerType.FileSystem:
                    directoryConnectionString = directoryConnectionString.TrimEnd('/');
                    directoryConnectionString += "/" + shardName;
                    var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directoryConnectionString);
                    var fsDirectory = FSDirectory.Open(folderPath);
                    directory = fsDirectory;
                    break;
                default:
                    break;
            }
            if (directory == null) return null;
            if (!IndexReader.IndexExists(directory))
            {
                using (new IndexWriter(directory, new StandardAnalyzer(LuceneConfiguration.LuceneVersion), true, IndexWriter.MaxFieldLength.UNLIMITED))
                {

                }
            }
            shard = new IndexShard();
            shard.Name = shardName;
            shard.Directory = directory;
            _shardCollections.TryAdd(shard.Name, shard);
            return shard;
        }

        public IndexShard LocateShard(IndexRequestItem indexRequest)
        {
            BuildServices();
            IContent content = null;
            if (indexRequest.Content != null) content = indexRequest.Content;
            else if (indexRequest.ContentId != 0)
            {
                _contentRepository.TryGet(new ContentReference(indexRequest.ContentId), out content);
            }
            if (content != null)
            {
                var site = _siteDefinitionResolver.GetByContent(content.ContentLink, true, true);
                if (site != null)
                {
                    return GetOrCreateShard(site.StartPage.ToReferenceWithoutVersion().ToString());
                }
            }
            return null;
        }
        private void BuildServices()
        {
            if (_contentRepository == null || _siteDefinitionResolver == null)
            {
                _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
                _siteDefinitionResolver = ServiceLocator.Current.GetInstance<ISiteDefinitionResolver>();
            }
        }
        public void WarmupShards()
        {

        }
    }
}