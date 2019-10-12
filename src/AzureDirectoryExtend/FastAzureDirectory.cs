using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using EPiServer.DynamicLuceneExtensions.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend
{
    public class FastAzureDirectory : AzureDirectory
    {
        private ConcurrentDictionary<string, BlobMeta> _blobMetaLookup;
        private readonly string _rootFolder;

        public FastAzureDirectory(CloudStorageAccount storageAccount,
      string containerName,
      Lucene.Net.Store.Directory cacheDirectory,
      string rootFolder) : base(storageAccount, containerName, cacheDirectory, false, rootFolder)
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                this._rootFolder = string.Empty;
            }
            else
            {
                rootFolder = rootFolder.Trim('/');
                this._rootFolder = rootFolder + "/";
            }
        }

        public FastAzureDirectory(CloudStorageAccount storageAccount)
      : base(storageAccount, (string)null, (Lucene.Net.Store.Directory)null)
        {
        }

        public FastAzureDirectory(CloudStorageAccount storageAccount, string catalog)
          : base(storageAccount, catalog, (Lucene.Net.Store.Directory)null)
        {
        }

        public FastAzureDirectory(
      CloudStorageAccount storageAccount,
      string catalog,
      Lucene.Net.Store.Directory cacheDirectory) : base(storageAccount, catalog, cacheDirectory)
        {
        }

        public override bool FileExists(string name)
        {
            BlobMeta meta;
            if (_blobMetaLookup.TryGetValue(name, out meta)) return true;
            return base.FileExists(name);
        }
        public override string[] ListAll()
        {
            if (_blobMetaLookup == null) ReloadMetadata();
            var blobList = BlobContainer.ListBlobs(_rootFolder, true, BlobListingDetails.All);
            var blobMetas = _blobMetaLookup;
            foreach (var meta in blobList)
            {
                if (meta is CloudBlockBlob)
                {
                    var fullBlob = (CloudBlockBlob)meta;
                    var newMeta = new BlobMeta(fullBlob);
                    if (newMeta.Name == null) continue;
                    BlobMeta oldMeta;
                    if (blobMetas.TryGetValue(newMeta.Name, out oldMeta))
                    {
                        if (newMeta.Length != oldMeta.Length || newMeta.LastModified != oldMeta.LastModified)
                        {
                            BlobMeta tmp;
                            if (blobMetas.TryRemove(newMeta.Name, out tmp))
                            {
                                blobMetas.TryAdd(newMeta.Name, newMeta);
                            }
                        }
                    }
                    else
                    {
                        blobMetas.TryAdd(newMeta.Name, newMeta);
                    }
                }
            }
            return blobList.Select(blob => blob.Uri.AbsolutePath.Substring(blob.Uri.AbsolutePath.LastIndexOf('/') + 1)).ToArray();
        }
        public override IndexInput OpenInput(string name)
        {
            if (_blobMetaLookup == null) ReloadMetadata();
            try
            {
                BlobMeta blobMeta;
                if (_blobMetaLookup.TryGetValue(name, out blobMeta) && blobMeta.HasData)
                {
                    return new FastAzureIndexInput(this, blobMeta);
                }
                CloudBlockBlob blockBlobReference = this.BlobContainer.GetBlockBlobReference(_rootFolder + name);
                blockBlobReference.FastFetchAttribute();
                BlobMeta newMeta;
                var input = new FastAzureIndexInput(this, blockBlobReference, out newMeta);
                if (newMeta.HasData)
                {
                    BlobMeta tmp;
                    if (_blobMetaLookup.TryRemove(name, out tmp))
                    {
                        _blobMetaLookup.TryAdd(name, newMeta);
                    }
                }
                return input;
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException(name, ex);
            }
        }

        public override IndexOutput CreateOutput(string name)
        {
            return new FastAzureIndexOutput(this, BlobContainer.GetBlockBlobReference(_rootFolder + name));
        }

        public void ReloadMetadata()
        {
            _blobMetaLookup = ListBlobsMeta();
        }

        private ConcurrentDictionary<string, BlobMeta> ListBlobsMeta()
        {
            var blobMetas = new ConcurrentDictionary<string, BlobMeta>();
            var blobList = BlobContainer.ListBlobs(null, true, BlobListingDetails.All);
            foreach (var meta in blobList)
            {
                if (meta is CloudBlockBlob)
                {
                    var fullBlob = (CloudBlockBlob)meta;
                    blobMetas.TryAdd(fullBlob.Name, new BlobMeta(fullBlob));
                }
            }
            return blobMetas;
        }
    }

    public class BlobMeta
    {
        public bool HasData { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime LastModified { get; set; }
        public ICloudBlob Blob { get; set; }
        public BlobMeta()
        {

        }
        public BlobMeta(CloudBlockBlob blob)
        {
            if (blob.Name.EndsWith(".lock")) return;
            long result1 = blob.Properties.Length;
            long.TryParse(blob.Metadata["CachedLength"], out result1);
            long result2 = 0;
            DateTime dateTime = blob.Properties.LastModified.Value.UtcDateTime;
            if (long.TryParse(blob.Metadata["CachedLastModified"], out result2))
            {
                if (result2 > FastAzureIndexInput.ticks1970)
                    result2 -= FastAzureIndexInput.ticks1970;
                dateTime = new DateTime(result2).ToUniversalTime();
            }
            this.Blob = blob;
            this.Name = blob.Name;
            this.LastModified = dateTime;
            this.Length = result1;
        }
    }
}