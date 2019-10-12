using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using EPiServer.DynamicLuceneExtensions.Extensions;
using System;
using System.IO;
using System.Threading;
namespace EPiServer.DynamicLuceneExtensions.AzureDirectoryExtend
{
    /// <summary>
    /// 100% replicate from FastAzureIndexInput, replace DownloadToStream with ParallelDownloadBlob
    /// </summary>
    public class FastAzureIndexInput : IndexInput
    {
        public static long ticks1970 = new DateTime(1970, 1, 1, 0, 0, 0).Ticks / 10000L;
        private AzureDirectory _azureDirectory;
        private CloudBlobContainer _blobContainer;
        private ICloudBlob _blob;
        private string _name;
        private IndexInput _indexInput;
        private Mutex _fileMutex;

        public Lucene.Net.Store.Directory CacheDirectory
        {
            get
            {
                return this._azureDirectory.CacheDirectory;
            }
        }
        public FastAzureIndexInput(AzureDirectory azuredirectory, BlobMeta blobMeta)
        {
            this._name = blobMeta.Name;
            this._fileMutex = BlobMutexManager.GrabMutex(this._name);
            this._fileMutex.WaitOne();
            try
            {
                this._azureDirectory = azuredirectory;
                this._blobContainer = azuredirectory.BlobContainer;
                this._blob = blobMeta.Blob;
                string name = this._name;
                bool flag = false;
                if (!this.CacheDirectory.FileExists(name))
                {
                    flag = true;
                }
                else
                {
                    long num = this.CacheDirectory.FileLength(name);
                    long result1 = blobMeta.Length;
                    DateTime dateTime = blobMeta.LastModified;
                    if (num != result1)
                    {
                        flag = true;
                    }
                    else
                    {
                        long ticks = this.CacheDirectory.FileModified(name);
                        if (ticks > FastAzureIndexInput.ticks1970)
                            ticks -= FastAzureIndexInput.ticks1970;
                        DateTime universalTime = new DateTime(ticks, DateTimeKind.Local).ToUniversalTime();
                        if (universalTime != dateTime && dateTime.Subtract(universalTime).TotalSeconds > 1.0)
                            flag = true;
                    }
                }
                if (flag)
                {
                    StreamOutput cachedOutputAsStream = this._azureDirectory.CreateCachedOutputAsStream(name);
                    this._blob.ParallelDownloadBlob((Stream)cachedOutputAsStream);
                    cachedOutputAsStream.Flush();
                    cachedOutputAsStream.Close();
                    this._indexInput = this.CacheDirectory.OpenInput(name);
                }
                else
                    this._indexInput = this.CacheDirectory.OpenInput(name);
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }
        public FastAzureIndexInput(AzureDirectory azuredirectory, ICloudBlob blob, out BlobMeta meta)
        {
            meta = new BlobMeta();
            this._name = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
            this._fileMutex = BlobMutexManager.GrabMutex(this._name);
            this._fileMutex.WaitOne();
            try
            {
                this._azureDirectory = azuredirectory;
                this._blobContainer = azuredirectory.BlobContainer;
                this._blob = blob;
                string name = this._name;
                bool flag = false;
                if (!this.CacheDirectory.FileExists(name))
                {
                    flag = true;
                }
                else
                {
                    long num = this.CacheDirectory.FileLength(name);
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
                    if (num != result1)
                    {
                        flag = true;
                    }
                    else
                    {
                        long ticks = this.CacheDirectory.FileModified(name);
                        if (ticks > FastAzureIndexInput.ticks1970)
                            ticks -= FastAzureIndexInput.ticks1970;
                        DateTime universalTime = new DateTime(ticks, DateTimeKind.Local).ToUniversalTime();
                        if (universalTime != dateTime && dateTime.Subtract(universalTime).TotalSeconds > 1.0)
                            flag = true;
                    }
                    meta.Name = this._name;
                    meta.LastModified = dateTime;
                    meta.Length = result1;
                    meta.Blob = blob;
                }
                if (flag)
                {
                    StreamOutput cachedOutputAsStream = this._azureDirectory.CreateCachedOutputAsStream(name);
                    this._blob.ParallelDownloadBlob((Stream)cachedOutputAsStream);
                    cachedOutputAsStream.Flush();
                    cachedOutputAsStream.Close();
                    this._indexInput = this.CacheDirectory.OpenInput(name);
                }
                else
                    this._indexInput = this.CacheDirectory.OpenInput(name);
                meta.HasData = true;
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }

        public FastAzureIndexInput(AzureDirectory azuredirectory, ICloudBlob blob)
        {
            this._name = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
            this._fileMutex = BlobMutexManager.GrabMutex(this._name);
            this._fileMutex.WaitOne();
            try
            {
                this._azureDirectory = azuredirectory;
                this._blobContainer = azuredirectory.BlobContainer;
                this._blob = blob;
                string name = this._name;
                bool flag = false;
                if (!this.CacheDirectory.FileExists(name))
                {
                    flag = true;
                }
                else
                {
                    long num = this.CacheDirectory.FileLength(name);
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
                    if (num != result1)
                    {
                        flag = true;
                    }
                    else
                    {
                        long ticks = this.CacheDirectory.FileModified(name);
                        if (ticks > FastAzureIndexInput.ticks1970)
                            ticks -= FastAzureIndexInput.ticks1970;
                        DateTime universalTime = new DateTime(ticks, DateTimeKind.Local).ToUniversalTime();
                        if (universalTime != dateTime && dateTime.Subtract(universalTime).TotalSeconds > 1.0)
                            flag = true;
                    }
                }
                if (flag)
                {
                    StreamOutput cachedOutputAsStream = this._azureDirectory.CreateCachedOutputAsStream(name);
                    this._blob.ParallelDownloadBlob((Stream)cachedOutputAsStream);
                    cachedOutputAsStream.Flush();
                    cachedOutputAsStream.Close();
                    this._indexInput = this.CacheDirectory.OpenInput(name);
                }
                else
                    this._indexInput = this.CacheDirectory.OpenInput(name);
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }

        public FastAzureIndexInput(FastAzureIndexInput cloneInput)
        {
            this._fileMutex = BlobMutexManager.GrabMutex(cloneInput._name);
            this._fileMutex.WaitOne();
            try
            {
                this._azureDirectory = cloneInput._azureDirectory;
                this._blobContainer = cloneInput._blobContainer;
                this._blob = cloneInput._blob;
                this._indexInput = cloneInput._indexInput.Clone() as IndexInput;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }

        public override byte ReadByte()
        {
            return this._indexInput.ReadByte();
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            this._indexInput.ReadBytes(b, offset, len);
        }

        public override long FilePointer
        {
            get
            {
                return this._indexInput.FilePointer;
            }
        }

        public override void Seek(long pos)
        {
            this._indexInput.Seek(pos);
        }

        protected override void Dispose(bool disposing)
        {
            this._fileMutex.WaitOne();
            try
            {
                this._indexInput.Dispose();
                this._indexInput = (IndexInput)null;
                this._azureDirectory = (AzureDirectory)null;
                this._blobContainer = (CloudBlobContainer)null;
                this._blob = (ICloudBlob)null;
                GC.SuppressFinalize((object)this);
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }

        public override long Length()
        {
            return this._indexInput.Length();
        }

        public override object Clone()
        {
            IndexInput indexInput = (IndexInput)null;
            try
            {
                this._fileMutex.WaitOne();
                indexInput = (IndexInput)new FastAzureIndexInput(this);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
            return (object)indexInput;
        }
    }
}