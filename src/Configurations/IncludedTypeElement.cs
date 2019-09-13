using System.Configuration;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class IncludedTypeElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = (object)value;
            }
        }

        [ConfigurationProperty("type", IsKey = true, IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)this["type"];
            }
            set
            {
                this["type"] = (object)value;
            }
        }

        [ConfigurationProperty("includedFields")]
        public IncludeFieldsCollection IncludedFields
        {
            get
            {
                return (IncludeFieldsCollection)this["includedFields"];
            }
        }

        [ConfigurationProperty("indexAllFields")]
        public bool IndexAllFields
        {
            get
            {
                if (string.IsNullOrEmpty(this["indexAllFields"] + "")) return false;
                return bool.Parse(this["indexAllFields"] + "");
            }
            set
            {
                this["indexAllFields"] = (object)value;
            }
        }
    }
}