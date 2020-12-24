namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspaceSettings
    {
        public MixinWorkspaceSettings(string outputPath, DumpTargetType dumpTargets, string mixinHandlerName = "mixin",
            bool experimentalInlineHandlers = false,
            string outputSuffix = "",
            GenerationType isGenerateOnly = GenerationType.None)
        {
            OutputPath = outputPath;
            MixinHandlerName = mixinHandlerName;
            DumpTargets = dumpTargets;
            GenerationType = isGenerateOnly;
            ExperimentalInlineHandlers = experimentalInlineHandlers;
            OutputSuffix = outputSuffix;
        }

        public string OutputPath { get; }

        public string MixinHandlerName { get; }

        public DumpTargetType DumpTargets { get; }

        public bool ExperimentalInlineHandlers { get; }

        public string OutputSuffix { get; }

        public GenerationType GenerationType { get; set; }
        
        public bool IsGeneratingHelperCode => GenerationType == GenerationType.HelperCode;
        
        public bool IsGeneratingMapped => GenerationType == GenerationType.Mapped;
    }
    
    public enum GenerationType
    {
        None,
        HelperCode,
        Mapped
    }
}