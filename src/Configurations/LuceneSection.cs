using System.Configuration;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class LuceneSection : ConfigurationSection
    {
        [ConfigurationProperty("active")]
        public bool Active
        {
            get
            {
                if (this["active"] == null) return false;
                return bool.Parse(this["active"] + "");
            }
            set
            {
                this["name"] = (object)value;
            }
        }

        [ConfigurationProperty("fieldPrefix")]
        public string Prefix
        {
            get
            {
                if (string.IsNullOrEmpty(this["fieldPrefix"] + "")) return "";
                return (string)this["fieldPrefix"];
            }
            set
            {
                this["fieldPrefix"] = (object)value;
            }
        }

        [ConfigurationProperty("indexAllTypes")]
        public bool IndexAllTypes
        {
            get
            {
                if (string.IsNullOrEmpty(this["indexAllTypes"] + "")) return false;
                return bool.Parse(this["indexAllTypes"] + "");
            }
            set
            {
                this["indexAllTypes"] = (object)value;
            }
        }

        [ConfigurationProperty("luceneVersion")]
        public string LuceneVersion
        {
            get
            {
                if (string.IsNullOrEmpty(this["luceneVersion"] + "")) return Lucene.Net.Util.Version.LUCENE_30.ToString();
                return this["luceneVersion"] + "";
            }
            set
            {
                this["indexAllTypes"] = (object)value;
            }
        }

        [ConfigurationProperty("includedTypes")]
        public IncludedTypesCollection IncludedTypes
        {
            get
            {
                return (IncludedTypesCollection)this["includedTypes"];
            }
        }
    }
}