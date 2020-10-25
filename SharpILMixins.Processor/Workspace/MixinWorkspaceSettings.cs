using System;

namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspaceSettings
    {
        public MixinWorkspaceSettings(string outputPath, bool shouldDumpTargets = false, string mixinHandlerName = "mixin",
            bool experimentalInlineHandlers = false)
        {
            OutputPath = outputPath;
            MixinHandlerName = mixinHandlerName;
            ShouldDumpTargets = shouldDumpTargets;
            ExperimentalInlineHandlers = experimentalInlineHandlers;
        }

        public string OutputPath { get; }
        public string MixinHandlerName { get; }

        public bool ShouldDumpTargets { get; }

        public bool ExperimentalInlineHandlers { get; }
    }
}