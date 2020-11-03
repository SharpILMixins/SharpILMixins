using System;
using System.IO;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Annotations.Parameters;

namespace SampleProjectCore.Mixins
{
    [Mixin(typeof(InjectClass))]
    public class MixinInjectClass
    {
        [Inject(At = AtLocation.Head, Method = "Example")]
        private void InjectHead()
        {
        }

        [Inject(At = AtLocation.Invoke, Method = InjectClassTargets.Methods.Example,
            Target = InjectClassTargets.Methods.ExampleInjects.Console_WriteLine_String, Shift = Shift.After)]
        private void InjectShifted()
        {
        }

        [Inject(At = AtLocation.Field, Method = "Example", Target = "System.Double SampleProjectCore.InjectClass::_d")]
        private void InjectField()
        {
        }


        [Inject(Method = "Example", At = AtLocation.Constant, ConstantValue = 0.01d)]
        private void InjectConstant()
        {
        }

        [Inject(Method = "Example", At = AtLocation.Constant, ConstantValue = "In")]
        private void InjectConstant2()
        {
        }


        [Inject(Method = "Example", At = AtLocation.Invoke, Target = "System.Int32 System.Random::Next()")]
        private void InjectInvoke()
        {
        }

        [Inject(Method = "Example", At = AtLocation.Return)]
        private void InjectReturn()
        {
        }

        [Inject(Method = "Example", At = AtLocation.Tail)]
        private void InjectTail()
        {
        }
    }
}