using System;
using System.Collections.Generic;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl
{
    public static class BaseMixinActionProcessorManager
    {
        static BaseMixinActionProcessorManager()
        {
            ActionProcessors.Add(typeof(InjectAttribute), w => new InjectionActionProcessor(w));
            ActionProcessors.Add(typeof(OverwriteAttribute), w => new OverwriteActionProcessor(w));
            ActionProcessors.Add(typeof(RedirectAttribute), w => new RedirectActionProcessor(w));
        }

        private static Dictionary<Type, Func<MixinWorkspace, IBaseMixinActionProcessor>> ActionProcessors { get; } =
            new();

        public static IBaseMixinActionProcessor GetProcessor(Type attributeType, MixinWorkspace workspace)
        {
            return ActionProcessors.GetValueOrDefault(attributeType)?.Invoke(workspace) ??
                   throw new MixinApplyException($"Unable to find processor for {attributeType.FullName}");
        }
    }
}