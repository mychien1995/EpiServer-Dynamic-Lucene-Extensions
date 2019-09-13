using System;
using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Models.Indexing
{
    public class ContentIndexResult
    {
        public ContentIndexResult()
        {
            IsSuccess = true;
        }
        public ContentIndexResult(Exception ex)
        {
            IsSuccess = false;
            var str = new StringBuilder(ex.Message);
            str.AppendLine(ex.StackTrace);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                str.AppendLine(ex.StackTrace);
            }

        }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}