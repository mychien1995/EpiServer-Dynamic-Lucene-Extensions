using EPiServer.Core;
using Lucene.Net.Analysis;
using Field = Lucene.Net.Documents.Field;
namespace EPiServer.DynamicLuceneExtensions.Indexing
{
    public interface IComputedField
    {
        Analyzer Analyzer { get; set; }
        Field.Store Store { get; set; }
        Field.Index Index { get; set; }
        Field.TermVector Vector { get; set; }
        LuceneFieldType DataType { get; set; }
        object GetValue(IContent content, string fieldName);
    }

    public abstract class IndexableComputedField : IComputedField
    {
        public Analyzer Analyzer { get; set; }
        public Field.Store Store { get; set; }
        public Field.Index Index { get; set; }
        public Field.TermVector Vector { get; set; }
        public LuceneFieldType DataType { get; set; }

        public abstract object GetValue(IContent content, string fieldName);
    }
}