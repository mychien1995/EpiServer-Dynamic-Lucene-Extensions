using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Lucene.Net.Store.Azure;
using EPiServer.DynamicLuceneExtensions.Extensions;

namespace Lucene.Net.Store.Azure
{
    public class FastAzureIndexOutput : IndexOutput
    {
        private AzureDirectory _azureDirectory;
        private CloudBlobContainer _blobContainer;
        private string _name;
        private IndexOutput _indexOutput;
        private Mutex _fileMutex;
        private ICloudBlob _blob;

        public Lucene.Net.Store.Directory CacheDirectory
        {
            get
            {
                return this._azureDirectory.CacheDirectory;
            }
        }

        public FastAzureIndexOutput(AzureDirectory azureDirectory, ICloudBlob blob)
        {
            this._fileMutex = BlobMutexManager.GrabMutex(this._name);
            this._fileMutex.WaitOne();
            try
            {
                this._azureDirectory = azureDirectory;
                this._blobContainer = this._azureDirectory.BlobContainer;
                this._blob = blob;
                this._name = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                this._indexOutput = this.CacheDirectory.CreateOutput(this._name);
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }

        public override void Flush()
        {
            this._indexOutput.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            this._fileMutex.WaitOne();
            try
            {
                string name = this._name;
                this._indexOutput.Flush();
                long length = this._indexOutput.Length;
                this._indexOutput.Dispose();
                Stream source = !this._azureDirectory.ShouldCompressFile(this._name) ? (Stream)new StreamInput(this.CacheDirectory.OpenInput(name)) : (Stream)this.CompressStream(name, length);
                try
                {
                    this._blob.FastUpload(source);
                    this._blob.Metadata["CachedLength"] = length.ToString();
                    this._blob.Metadata["CachedLastModified"] = this.CacheDirectory.FileModified(name).ToString();
                    this._blob.SetMetadata((AccessCondition)null, (BlobRequestOptions)null, (OperationContext)null);
                }
                finally
                {
                    source.Dispose();
                }
                this._indexOutput = (IndexOutput)null;
                this._blobContainer = (CloudBlobContainer)null;
                this._blob = (ICloudBlob)null;
                GC.SuppressFinalize((object)this);
            }
            finally
            {
                this._fileMutex.ReleaseMutex();
            }
        }

        private MemoryStream CompressStream(string fileName, long originalLength)
        {
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                using (IndexInput indexInput = this.CacheDirectory.OpenInput(fileName))
                {
                    using (DeflateStream deflateStream = new DeflateStream((Stream)memoryStream, CompressionMode.Compress, true))
                    {
                        byte[] numArray = new byte[indexInput.Length()];
                        indexInput.ReadBytes(numArray, 0, numArray.Length);
                        deflateStream.Write(numArray, 0, numArray.Length);
                    }
                }
                memoryStream.Seek(0L, SeekOrigin.Begin);
                Debug.WriteLine(string.Format("COMPRESSED {0} -> {1} {2}% to {3}", (object)originalLength, (object)memoryStream.Length, (object)(float)((double)memoryStream.Length / (double)originalLength * 100.0), (object)this._name));
            }
            catch
            {
                memoryStream.Dispose();
                throw;
            }
            return memoryStream;
        }

        public override long Length
        {
            get
            {
                return this._indexOutput.Length;
            }
        }

        public override void WriteByte(byte b)
        {
            this._indexOutput.WriteByte(b);
        }

        public override void WriteBytes(byte[] b, int length)
        {
            this._indexOutput.WriteBytes(b, length);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            this._indexOutput.WriteBytes(b, offset, length);
        }

        public override long FilePointer
        {
            get
            {
                return this._indexOutput.FilePointer;
            }
        }

        public override void Seek(long pos)
        {
            this._indexOutput.Seek(pos);
        }
    }
}
