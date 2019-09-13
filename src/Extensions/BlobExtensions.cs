using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.DynamicLuceneExtensions.Extensions
{
    public static class BlobExtensions
    {
        public static void ParallelDownloadBlob(this ICloudBlob blob, Stream outPutStream)
        {
            blob.FetchAttributes();
            int bufferLength = 1 * 1024 * 1024;//1 MB chunk
            long blobRemainingLength = blob.Properties.Length;
            Queue<KeyValuePair<long, long>> queues = new Queue<KeyValuePair<long, long>>();
            long offset = 0;
            while (blobRemainingLength > 0)
            {
                long chunkLength = (long)Math.Min(bufferLength, blobRemainingLength);
                queues.Enqueue(new KeyValuePair<long, long>(offset, chunkLength));
                offset += chunkLength;
                blobRemainingLength -= chunkLength;
            }
            var downloadedTrunks = new List<DownloadChunk>();
            object _lock = new object();
            Task.WaitAll(queues.Select(queue =>
            {
                return Task.Run(() =>
                {
                    using (var ms = new MemoryStream())
                    {
                        blob.DownloadRangeToStream(ms, queue.Key, queue.Value);
                        lock (_lock)
                        {
                            downloadedTrunks.Add(new DownloadChunk()
                            {
                                OffSet = queue.Key,
                                Data = ms.ToArray()
                            });
                        }
                    }
                });
            }).ToArray());
            downloadedTrunks = downloadedTrunks.OrderBy(x => x.OffSet).ToList();
            foreach (var trunk in downloadedTrunks)
            {
                outPutStream.Position = trunk.OffSet;
                outPutStream.Write(trunk.Data, 0, trunk.Data.Length);
            }
        }
    }

    public class DownloadChunk
    {
        public long OffSet { get; set; }
        public byte[] Data { get; set; }
    }
}