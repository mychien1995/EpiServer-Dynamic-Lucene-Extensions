using System.Configuration;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class DirectoryElement : ConfigurationElement
    {
        [ConfigurationProperty("connectionString")]
        public string ConnectionString
        {
            get
            {
                return (string)this["connectionString"];
            }
            set
            {
                this["connectionString"] = (object)value;
            }
        }

        [ConfigurationProperty("containerName")]
        public string ContainerName
        {
            get
            {
                return (string)this["containerName"];
            }
            set
            {
                this["containerName"] = (object)value;
            }
        }

        [ConfigurationProperty("type")]
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
    }
}