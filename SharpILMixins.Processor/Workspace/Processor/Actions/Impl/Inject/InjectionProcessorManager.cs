using System.Collections.Generic;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject
{
    public static class InjectionProcessorManager
    {
        static InjectionProcessorManager()
        {
            Register(new HeadInjectionProcessor());
            Register(new ReturnInjectionProcessor());
            Register(new TailInjectionProcessor());
        }

        public static Dictionary<AtLocation, BaseInjectionProcessor> InjectionProcessors { get; } =
            new Dictionary<AtLocation, BaseInjectionProcessor>();

        private static void Register(BaseInjectionProcessor injectionProcessor)
        {
            InjectionProcessors.Add(injectionProcessor.Location, injectionProcessor);
        }
    }
}