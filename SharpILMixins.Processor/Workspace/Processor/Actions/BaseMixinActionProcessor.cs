using SharpILMixins.Annotations;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public abstract class BaseMixinActionProcessor<TAttribute> : IBaseMixinActionProcessor where TAttribute: BaseMixinAttribute
    {
        public abstract void ProcessAction(MixinAction action, TAttribute attribute);
        
        public void ProcessAction(MixinAction action, BaseMixinAttribute attribute)
        {
            if (attribute is TAttribute tAttribute)
            {
                ProcessAction(action, tAttribute);
            }
        }
    }
}