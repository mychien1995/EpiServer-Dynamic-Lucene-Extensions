using EPiServer.DynamicLuceneExtensions.Helpers;

namespace EPiServer.DynamicLuceneExtensions.Queries
{
    public class AccessControlListQuery : CollectionQueryBase
    {
        public AccessControlListQuery()
          : this(LuceneOperator.OR)
        {
        }

        public AccessControlListQuery(LuceneOperator innerOperator)
          : base(ContentIndexHelpers.GetIndexFieldName(Constants.INDEX_FIELD_NAME_ACL), innerOperator)
        {
        }

        public void AddRole(string roleName)
        {
            this.Items.Add("G:" + roleName);
        }

        public void AddUser(string userName)
        {
            this.Items.Add("U:" + userName);
        }
    }
}