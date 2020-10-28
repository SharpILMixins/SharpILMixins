using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;

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
    }
}