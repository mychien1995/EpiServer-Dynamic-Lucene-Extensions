using System.Configuration;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    [ConfigurationCollection(typeof(IncludedFieldElement))]
    public class IncludeFieldsCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        public IncludedFieldElement this[int index]
        {
            get
            {
                return (IncludedFieldElement)this.BaseGet(index);
            }
            set
            {
                if (this.BaseGet(index) != null)
                    this.BaseRemoveAt(index);
                this.BaseAdd(index, value);
            }
        }

        public void Add(IncludedFieldElement element)
        {
            this.BaseAdd(element);
        }

        public void Clear()
        {
            this.BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return (IncludedFieldElement)new IncludedFieldElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (object)((IncludedFieldElement)element).Name;
        }

        public void Remove(IncludedFieldElement element)
        {
            this.BaseRemove((object)element.Name);
        }

        public void Remove(string name)
        {
            this.BaseRemove((object)name);
        }

        public void RemoveAt(int index)
        {
            this.BaseRemoveAt(index);
        }
    }
}