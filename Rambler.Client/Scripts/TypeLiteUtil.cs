namespace Rambler.Client.Scripts
{
    using System;
    using System.Linq;
    using TypeLite;
    using Rambler.Contracts;

    /// <summary>
    /// Helper for generating the typescript definitions via reflection
    /// </summary>
    public static class TypeLiteUtil
    {
        public static string GuidString()
        {
            //quick hack - guids are strings on the client
            return @"
declare namespace System {
    type Guid = string;
}";
        }

        public static string Generate()
        {
            //get the stuff we care about
            var assembly = typeof(MessageKey).Assembly;
            var types = assembly
                .GetTypes()
                .Where(x => x.Namespace != null)
                .Where(x => x.Namespace.EndsWith("Requests") || x.Namespace.EndsWith("Responses") || x.Namespace.EndsWith("Api"))
                //.Where(x => !x.IsAbstract && !x.IsGenericTypeDefinition && x.IsClass)
                //.Where(x=> !x.GetInterfaces().Any())
                .ToArray();

            var ts = TypeScript.Definitions();

            //add all the types!
            foreach (var t in types) ts.For(t).ToModule("Rambler");
            ts.For<Guid>().Ignore();

            return ts.Generate(TsGeneratorOutput.Properties);
        }
    }
}