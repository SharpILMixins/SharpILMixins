using System.Collections;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Analyzer.Utils
{
    public static class Utilities
    {
        public const string Category = "Mixin";

        public static string GetMixinCode(int code)
        {
            return $"MIXN{code.ToString().PadLeft(3, '0')}";
        }

        public static bool IsMixinIncludedInWorkspace(this MixinConfiguration configuration, string mixinType)
        {
            return configuration.Mixins.Any(m =>
            {
                var isMixinWithNamespaceEquals = configuration.BaseNamespace != null &&
                              (configuration.BaseNamespace + "." + m).Equals(mixinType);
                return m.Equals(mixinType) || isMixinWithNamespaceEquals;
            });
        }

        [CanBeNull]
        public static MixinConfiguration? GetMixinConfiguration(ImmutableArray<AdditionalText> additionalFiles,
            CancellationToken cancellation)
        {
            var firstOrDefault = additionalFiles.FirstOrDefault(t => Path.GetFileName(t.Path).Equals("mixins.json"));
            var sourceText = firstOrDefault?.GetText(cancellation);
            return sourceText == null ? null : JsonConvert.DeserializeObject<MixinConfiguration>(sourceText.ToString());
        }
    }
}