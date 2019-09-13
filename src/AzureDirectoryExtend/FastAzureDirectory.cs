using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;

namespace EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend
{
    /// <summary>
    /// 100% replicate from AzureDirectory, replace AzureIndexInput with FastAzureIndexInput
    /// </summary>
    public class FastAzureDirectory : AzureDirectory
    {
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

        public override IndexInput OpenInput(string name)
        {
            try
            {
                CloudBlockBlob blockBlobReference = this.BlobContainer.GetBlockBlobReference(name);
                blockBlobReference.FetchAttributes((AccessCondition)null, (BlobRequestOptions)null, (OperationContext)null);
                return (IndexInput)new FastAzureIndexInput(this, blockBlobReference);
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException(name, ex);
            }
        }
    }
}