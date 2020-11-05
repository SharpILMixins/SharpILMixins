using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using SharpILMixins.Processor.Workspace;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            SourceText? sourceText;
            try
            {
                var firstOrDefault = additionalFiles.FirstOrDefault(t => Path.GetFileName(t.Path).Equals("mixins.json"));
                sourceText = firstOrDefault?.GetText(cancellation);
            }
            catch
            {
                sourceText = null;
            }
            return sourceText == null ? null : JsonConvert.DeserializeObject<MixinConfiguration>(sourceText.ToString());
        }

        public static DiagnosticDescriptor ProcessRuleForRider(DiagnosticDescriptor diagnosticDescriptor)
        {
            return diagnosticDescriptor;
        }

        public static (TextDocument? firstOrDefault, MixinConfiguration?) GetMixinConfiguration(
            IEnumerable<TextDocument> additionalFiles)
        {
            var firstOrDefault =
                additionalFiles.FirstOrDefault(t => Path.GetFileName(t.FilePath).Equals("mixins.json"));
            SourceText? sourceText = null;
            firstOrDefault?.TryGetText(out sourceText);
            return sourceText == null
                ? (null, null)
                : (firstOrDefault, JsonConvert.DeserializeObject<MixinConfiguration>(sourceText.ToString()));
        }

        public static async Task<Solution> ModifyMixinWorkspace(Action<MixinConfiguration> modifyMixins,
            Solution solution, IEnumerable<TextDocument> additionalDocuments)
        {
            await Task.Yield();

            var (configurationDocument, existingConfiguration) =
                GetMixinConfiguration(additionalDocuments);

            if (configurationDocument == null || existingConfiguration == null) return solution;

            modifyMixins(existingConfiguration);

            return solution
                .WithAdditionalDocumentText(
                    configurationDocument.Id,
                    SourceText.From(JsonConvert.SerializeObject(existingConfiguration, Formatting.Indented))
                );
        }
    }
}