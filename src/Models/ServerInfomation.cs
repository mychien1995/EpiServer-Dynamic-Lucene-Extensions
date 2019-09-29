using EPiServer.Data;
using EPiServer.Data.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Models
{
    public class ServerInfomation : IDynamicData
    {
        public Identity Id { get; set; }
        public string Name { get; set; }
        public Guid LocalRaiserId { get; set; }
        public long IndexSize { get; set; }
        public bool InHealthChecking { get; set; }
        public bool InRecovering { get; set; }
    }
}