namespace SharpILMixins.Processor.Workspace
{
    public class WorkspaceSettings
    {
        public WorkspaceSettings(bool shouldDumpTargets, string mixinHandlerName, bool experimentalInlineHandlers)
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