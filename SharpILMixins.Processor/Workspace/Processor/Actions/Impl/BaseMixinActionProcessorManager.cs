using System;
using System.Collections.Generic;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl
{
    public static class BaseMixinActionProcessorManager
    {
        private static Dictionary<Type, IBaseMixinActionProcessor> ActionProcessors { get; }= new Dictionary<Type, IBaseMixinActionProcessor>();

        static BaseMixinActionProcessorManager()
        {
            ActionProcessors.Add(typeof(InjectAttribute), new InjectionActionProcessor());
            ActionProcessors.Add(typeof(OverwriteAttribute), new OverwriteActionProcessor());
        }

        public static IBaseMixinActionProcessor GetProcessor(Type attributeType)
        {
            return ActionProcessors.GetValueOrDefault(attributeType) ??
                   throw new MixinApplyException($"Unable to find processor for {attributeType.FullName}");
        }

    }
}