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

        [Inject(At = AtLocation.Invoke, Method = "Example", Target = "System.Void System.Console::WriteLine(System.String)", Shift = Shift.After)]
        private void InjectShifted()
        {

        }

        [Inject(At = AtLocation.Field, Method = "Example", Target = "System.Double SampleProjectCore.InjectClass::_d")]
        private int InjectField()
        {
            return 0;
        }

        
        [Inject(Method = "Example", At = AtLocation.Invoke, Target = "System.Int32 System.Random::Next()")]
        private void InjectInvoke() {
        }

        [Inject(Method = "Example", At = AtLocation.Return)]
        private void InjectReturn() {
        }

        [Inject(Method = "Example", At = AtLocation.Tail)]
        private void InjectTail() {
        }
    }
}