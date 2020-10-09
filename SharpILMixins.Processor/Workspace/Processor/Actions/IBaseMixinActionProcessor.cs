using SharpILMixins.Annotations;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public interface IBaseMixinActionProcessor
    {
        public MixinWorkspace Workspace { get; set; }

        void ProcessAction(MixinAction action, BaseMixinAttribute attribute);
    }
}