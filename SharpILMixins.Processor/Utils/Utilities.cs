using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using SharpILMixins.Annotations.Parameters;

namespace SharpILMixins.Processor.Utils
{
    public static class Utilities
    {
        public static bool DebugMode { get; set; } =

#if DEBUG
            true
#else
            false
#endif
            ;

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (var element in source)
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
        }


        public static string ReadResource(string name)
        {
            return ReadResource(Assembly.GetExecutingAssembly(), name);
        }

        public static string ReadResource(Assembly assembly, string name)
        {
            string resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));
            using Stream stream = assembly.GetManifestResourceStream(resourcePath) ??
                                  throw new InvalidOperationException($"Unable to find embedded file named {name}");
            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public static Resource? ReadResource(AssemblyDef assembly, string name)
        {
            return assembly.Modules.SelectMany(c => c.Resources).FirstOrDefault(r => r.Name.EndsWith(name));
        }

        public static bool IsDefault<T>(this T value) where T : struct
        {
            return value.Equals(default(T));
        }

        public static int GetMixinParameterCount(this MethodDef instance)
        {
            return instance.ParamDefs.Count(p => p.GetCustomAttribute<BaseParameterAttribute>() != null);
        }

        public static string GenerateRandomName(string prefix = "mixin")
        {
            return $"{(!string.IsNullOrEmpty(prefix) ? prefix + "$" : "")}{Guid.NewGuid()}";
        }
    }
}