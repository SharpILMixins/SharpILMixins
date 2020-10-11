namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspaceSettings
    {
        public MixinWorkspaceSettings(bool shouldDumpTargets, string mixinHandlerName, bool experimentalInlineHandlers)
        {
            MixinHandlerName = mixinHandlerName;
            ShouldDumpTargets = shouldDumpTargets;
            ExperimentalInlineHandlers = experimentalInlineHandlers;
        }

        public string MixinHandlerName { get; }

        public bool ShouldDumpTargets { get; }

        public bool ExperimentalInlineHandlers { get; }
    }
}