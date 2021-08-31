using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;

namespace SampleProjectCore.Mixins
{
    [Mixin(YieldReturnClassTargets.Methods.GetSomething_StateMachine)]
    public class MixinYieldReturnClass
    {
        [Inject(YieldReturnClassTargets.Methods.GetSomething_StateMachine_Method, AtLocation.Head)]
        public void Test()
        {

        }
    }
}