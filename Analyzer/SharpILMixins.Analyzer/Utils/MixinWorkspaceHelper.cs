using System.Linq;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Analyzer.Utils
{
    public static class MixinWorkspaceHelper
    {
        public static void AddMixin(ref MixinConfiguration configuration, string mixinType, bool isAtTop)
        {
            var toAdd = mixinType;

            var namespaceToAdd = configuration.BaseNamespace;
            if (namespaceToAdd != null)
                namespaceToAdd += ".";

            if (namespaceToAdd != null && mixinType.StartsWith(namespaceToAdd) &&
                namespaceToAdd.Length >= 0 && toAdd.Length > namespaceToAdd.Length)
                toAdd = toAdd.Substring(namespaceToAdd.Length);

            configuration.Mixins = (isAtTop
                ? new[] {toAdd}.Concat(configuration.Mixins)
                : configuration.Mixins.Concat(new[] {toAdd})).ToArray();
        }
    }
}