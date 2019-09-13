using System.Configuration;

namespace EPiServer.DynamicLuceneExtensions.Configurations
{
    [ConfigurationCollection(typeof(IncludedTypeElement), AddItemName = "includedType", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class IncludedTypesCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        public IncludedTypeElement this[int index]
        {
            get
            {
                return (IncludedTypeElement)this.BaseGet(index);
            }
            set
            {
                if (this.BaseGet(index) != null)
                    this.BaseRemoveAt(index);
                this.BaseAdd(index, value);
            }
        }

        public void Add(IncludedTypeElement element)
        {
            this.BaseAdd(element);
        }

        public void Clear()
        {
            this.BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return (IncludedTypeElement)new IncludedTypeElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (object)((IncludedTypeElement)element).Name;
        }

        public void Remove(IncludedTypeElement element)
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

        protected override void Init()
        {
            base.Init();
            this.AddElementName = "includedType";
        }

        protected override string ElementName => "includedType";
    }
}