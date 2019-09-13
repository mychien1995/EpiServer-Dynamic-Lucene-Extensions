using System;
using System.Configuration;
using Field = Lucene.Net.Documents.Field;
namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    public class IncludedFieldElement : ConfigurationElement
    {
        [ConfigurationProperty("name")]
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

        [ConfigurationProperty("analyzer")]
        public string Analyzer
        {
            get
            {
                if (string.IsNullOrEmpty(this["analyzer"] + ""))
                {
                    return Constants.LUCENE_WHITESPACE_ANALYZER;
                }
                return (string)this["analyzer"];
            }
            set
            {
                this["analyzer"] = (object)value;
            }
        }

        [ConfigurationProperty("store")]
        public string _store
        {
            get
            {
                return (string)this["store"];
            }
            set
            {
                this["store"] = (object)value;
            }
        }

        [ConfigurationProperty("index")]
        public string _index
        {
            get
            {
                return (string)this["index"];
            }
            set
            {
                this["index"] = (object)value;
            }
        }

        [ConfigurationProperty("vector")]
        public string _vector
        {
            get
            {
                return (string)this["vector"];
            }
            set
            {
                this["vector"] = (object)value;
            }
        }

        [ConfigurationProperty("dataType")]
        public string _dataType
        {
            get
            {
                return (string)this["dataType"];
            }
            set
            {
                this["dataType"] = (object)value;
            }
        }


        public Field.Store Store
        {
            get
            {
                if (string.IsNullOrEmpty(_store) || (_store + "").ToLower() == "yes")
                {
                    return Field.Store.YES;
                }
                return Field.Store.NO;
            }
        }

        public Field.Index Index
        {
            get
            {
                if (string.IsNullOrEmpty(_index) || (_index + "").ToLower() == "yes")
                {
                    return Field.Index.ANALYZED;
                }
                return Field.Index.NO;
            }
        }

        public Field.TermVector Vector
        {
            get
            {
                if (string.IsNullOrEmpty(_vector) || (_vector + "").ToLower() == "yes")
                {
                    return Field.TermVector.YES;
                }
                return Field.TermVector.NO;
            }
        }

        public LuceneFieldType DataType
        {
            get
            {
                if (string.IsNullOrEmpty(_dataType))
                {
                    return LuceneFieldType.String;
                }
                return (LuceneFieldType)Enum.Parse(typeof(LuceneFieldType), _dataType);
            }
        }
    }
}