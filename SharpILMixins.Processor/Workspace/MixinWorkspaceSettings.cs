namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspaceSettings
    {
        public MixinWorkspaceSettings(string outputPath, DumpTargetType dumpTargets, string mixinHandlerName = "mixin",
            bool experimentalInlineHandlers = false,
            string outputSuffix = "",
            bool isGenerateOnly = true)
        {
            OutputPath = outputPath;
            MixinHandlerName = mixinHandlerName;
            DumpTargets = dumpTargets;
            IsGenerateOnly = isGenerateOnly;
            ExperimentalInlineHandlers = experimentalInlineHandlers;
            OutputSuffix = outputSuffix;
        }

        public string OutputPath { get; }

        public string MixinHandlerName { get; }

        public DumpTargetType DumpTargets { get; }

        public bool ExperimentalInlineHandlers { get; }

        public string OutputSuffix { get; }

        public bool IsGenerateOnly { get; set; }
    }
}