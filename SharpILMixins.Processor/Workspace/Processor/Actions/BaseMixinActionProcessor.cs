using SharpILMixins.Annotations;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public abstract class BaseMixinActionProcessor<TAttribute> : IBaseMixinActionProcessor
        where TAttribute : BaseMixinAttribute
    {
        protected BaseMixinActionProcessor(MixinWorkspace workspace)
        {
            Workspace = workspace;
        }

        public MixinWorkspace Workspace { get; set; }

        public void ProcessAction(MixinAction action, BaseMixinAttribute attribute)
        {
            if (attribute.GetType() == typeof(TAttribute)) ProcessAction(action, (TAttribute)attribute);
        }

        public abstract void ProcessAction(MixinAction action, TAttribute attribute);
    }
}