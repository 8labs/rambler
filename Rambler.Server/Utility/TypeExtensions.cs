namespace Rambler.Server.Utility
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;

    public static class TypeExtensions
    {
        public static T GetAttribute<T>(this Type type)
            where T : Attribute
        {
            return type.GetTypeInfo()
                .GetCustomAttributes(false)
                .OfType<T>()
                .FirstOrDefault();
        }

        public static IEnumerable<Type> GetAllTypesImplementingOpenGenericInterface(this Assembly assembly, Type openGenericType)
        {
            return assembly.GetTypes()
                .Select(t => new { type = t, info = t.GetTypeInfo() })
                .Where(t => !t.info.IsAbstract && !t.info.IsInterface)
                .Where(t => t.type.ImplementsOpenGenericInterface(openGenericType))
                .Select(t => t.type)
                ;
        }

        public static bool ImplementsOpenGenericInterface(this Type type, Type openGenericType)
        {
            return type
                .GetTypeInfo()
                .ImplementedInterfaces
                .Any(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == openGenericType);
        }
    }
}
