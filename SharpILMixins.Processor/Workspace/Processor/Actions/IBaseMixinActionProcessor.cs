using SharpILMixins.Annotations;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public interface IBaseMixinActionProcessor
    {
        void ProcessAction(MixinAction action, BaseMixinAttribute attribute);
    }
}