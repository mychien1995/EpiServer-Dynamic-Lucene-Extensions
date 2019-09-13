using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EPiServer.DynamicLuceneExtensions.Extensions
{
    public static class PropertyExtensions
    {
        public static bool IsPrimitive(this PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            if (propertyType.IsPrimitive) return true;
            return new[]
        {
                typeof (Enum),
                typeof (String),
                typeof (Char),
                typeof (Guid),
                typeof (Boolean),
                typeof (Byte),
                typeof (Int16),
                typeof (Int32),
                typeof (Int64),
                typeof (Single),
                typeof (Double),
                typeof (Decimal),
                typeof (SByte),
                typeof (UInt16),
                typeof (UInt32),
                typeof (UInt64),
                typeof (DateTime),
                typeof (DateTimeOffset),
                typeof (TimeSpan),
            }.Any(x => x == propertyType);
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        public static bool IsNumeric(this PropertyInfo property)
        {
            var type = property.PropertyType;
            return NumericTypes.Contains(type) ||
               NumericTypes.Contains(Nullable.GetUnderlyingType(type));
        }
    }
}