using System.Collections.Generic;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject
{
    public static class InjectionProcessorManager
    {
        public static Dictionary<AtLocation, BaseInjectionProcessor> InjectionProcessors { get; } = new Dictionary<AtLocation, BaseInjectionProcessor>();

        static InjectionProcessorManager()
        {
            Register(new HeadInjectionProcessor());
            Register(new ReturnInjectionProcessor());
        }

        private static void Register(BaseInjectionProcessor injectionProcessor)
        {
            InjectionProcessors.Add(injectionProcessor.Location, injectionProcessor);
        }
    }
}