using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Lucene.Net.Documents;
using EPiServer.DynamicLuceneExtensions.Attributes;
using EPiServer.DynamicLuceneExtensions.Helpers;
using EPiServer.DynamicLuceneExtensions.Models.Search;
using EPiServer.DynamicLuceneExtensions.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace EPiServer.DynamicLuceneExtensions.Extensions
{
    public static class LuceneQueryExtensions
    {
        public static IQueryExpression And(this IQueryExpression query, IQueryExpression anotherQuery)
        {
            var groupQuery = new GroupQuery(LuceneOperator.AND);
            groupQuery.QueryExpressions.Add(query);
            groupQuery.QueryExpressions.Add(anotherQuery);
            return groupQuery;
        }

        public static IQueryExpression Or(this IQueryExpression query, IQueryExpression anotherQuery)
        {
            var groupQuery = new GroupQuery(LuceneOperator.OR);
            groupQuery.QueryExpressions.Add(query);
            groupQuery.QueryExpressions.Add(anotherQuery);
            return groupQuery;
        }

        public static IQueryExpression Not(this IQueryExpression query, IQueryExpression anotherQuery)
        {
            var groupQuery = new GroupQuery(LuceneOperator.NOT);
            groupQuery.QueryExpressions.Add(query);
            groupQuery.QueryExpressions.Add(anotherQuery);
            return groupQuery;
        }

        public static IQueryExpression FilterByIdsToExclude(this IQueryExpression expression, IList<int> IdsToExclude)
        {
            if (IdsToExclude != null && IdsToExclude.Any())
            {
                foreach (var id in IdsToExclude)
                {
                    var fieldQuery = new FieldQuery(Constants.INDEX_FIELD_NAME_ID, ContentIndexHelpers.GetIndexFieldId(new ContentReference(id)), true);
                    expression = expression.Not(fieldQuery);
                }
            }
            return expression;
        }

        public static IQueryExpression FilterByIdsToInclude(this IQueryExpression expression, IList<int> IdsToInclude)
        {
            if (IdsToInclude != null && IdsToInclude.Any())
            {
                foreach (var id in IdsToInclude)
                {
                    var fieldQuery = new FieldQuery(Constants.INDEX_FIELD_NAME_ID, ContentIndexHelpers.GetIndexFieldId(new ContentReference(id)), true);
                    expression = expression.Or(fieldQuery);
                }
            }
            return expression;
        }

        public static IQueryExpression FilterByACL(this IQueryExpression expression)
        {
            var aclQuery = new AccessControlListQuery();
            var _virtualRoleRepository = ServiceLocator.Current.GetInstance<IVirtualRoleRepository>();
            var principal = PrincipalInfo.Current;
            var context = HttpContext.Current;
            if (principal?.Principal == null)
                return expression;

            aclQuery.AddUser(principal.Principal.Identity.Name);
            ClaimsPrincipal claimsPrincipal = principal.Principal as ClaimsPrincipal;
            IEnumerable<Claim> claims = claimsPrincipal != null ? claimsPrincipal.Claims.Where<Claim>(c => c.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")) : (IEnumerable<Claim>)null;
            if (claims == null)
                return expression;
            foreach (Claim claim in claims)
                aclQuery.AddRole(claim.Value);
            foreach (string allRole in _virtualRoleRepository.GetAllRoles())
            {
                VirtualRoleProviderBase virtualRole;
                if (_virtualRoleRepository.TryGetRole(allRole, out virtualRole) && virtualRole.IsInVirtualRole(principal.Principal, context))
                    aclQuery.AddRole(allRole);
            }
            return expression.And(aclQuery);
        }

        public static IQueryExpression FilterByPublished(this IQueryExpression expression)
        {
            var fieldQuery = new FieldQuery(Constants.INDEX_FIELD_NAME_STATUS, ((int)VersionStatus.Published).ToString());
            return expression.And(fieldQuery);
        }

        public static IQueryExpression FilterByLanguage(this IQueryExpression expression, string language)
        {
            var fieldQuery = new FieldQuery(Constants.INDEX_FIELD_NAME_LANGUAGE, language.ToString());
            return expression.And(fieldQuery);
        }

        public static IQueryExpression SearchInFields(this IQueryExpression expression, string fieldValue, params string[] fieldNames)
        {
            if (!string.IsNullOrEmpty(fieldValue))
            {
                var fieldGroupQuery = new GroupQuery(LuceneOperator.OR);
                foreach (var fieldName in fieldNames)
                {
                    fieldGroupQuery.QueryExpressions.Add(new FieldQuery(fieldName, fieldValue));
                }
                return expression.And(fieldGroupQuery);
            }
            return expression;
        }

        public static IQueryExpression FilterByAncestor(this IQueryExpression expression, ContentReference ancestor)
        {
            var virtualPathQuery = new VirtualPathQuery();
            virtualPathQuery.AddContentNodes(ancestor);
            return expression.And(virtualPathQuery);
        }

        public static IQueryExpression FilterByContentType<T>(this IQueryExpression expression, bool includeInherit = false) where T : ContentData
        {
            var contentTypeQuery = new ContentTypeQuery<T>(includeInherit);
            return expression.And(contentTypeQuery);
        }

        public static IQueryExpression SearchByStringList(this IQueryExpression expression, string fieldName, List<string> stringValues, LuceneOperator ops = LuceneOperator.AND)
        {
            if (stringValues.Any())
            {
                stringValues = stringValues.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToTagCodeFormat()).ToList();
                var groupQuery = new GroupQuery(ops);
                foreach (var str in stringValues)
                {
                    groupQuery.QueryExpressions.Add(new FieldQuery(fieldName, str));
                }
                return expression.And(groupQuery);
            }
            return expression;
        }

        public static IQueryExpression FilterByDateRange(this IQueryExpression expression, string fieldName, DateTime startDate, DateTime endDate, bool includeEmptyField = false)
        {
            var strStartDate = DateTools.DateToString(startDate, DateTools.Resolution.SECOND);
            var strEndDate = DateTools.DateToString(endDate, DateTools.Resolution.SECOND);
            var rangeQuery = new RangeQuery(strStartDate, strEndDate, fieldName, true);
            if (includeEmptyField)
            {
                var groupQuery = new GroupQuery(LuceneOperator.NOT);
                groupQuery.QueryExpressions.Add(new AllQuery());
                groupQuery.QueryExpressions.Add(new FieldQuery(fieldName, @"[* TO *]"));
                return expression.And(rangeQuery.Or(groupQuery));
            }
            return expression.And(rangeQuery);
        }

        public static IQueryExpression FilterByNonExpired(this IQueryExpression expression)
        {
            return expression.Not(new FieldQuery(Constants.INDEX_FIELD_NAME_EXPIRED, "true"));
        }

        public static IQueryExpression SearchInAllFields<T>(this IQueryExpression expression, string value)
        {
            if (string.IsNullOrEmpty(value)) return expression;
            var fieldGroupQuery = new GroupQuery(LuceneOperator.OR);
            var contentType = typeof(T);
            var properties = contentType.GetProperties();
            foreach (var property in properties)
            {
                var fieldName = ((IndexFieldNameAttribute)property.GetCustomAttributes(typeof(IndexFieldNameAttribute), true).FirstOrDefault())?.IndexFieldName
                    ?? property.Name;
                fieldGroupQuery.QueryExpressions.Add(new FieldQuery(fieldName, value));
            }
            return expression.And(fieldGroupQuery);
        }

        public static string ToTagCodeFormat(this string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("-", "").Replace(" ", "");
        }
    }
}