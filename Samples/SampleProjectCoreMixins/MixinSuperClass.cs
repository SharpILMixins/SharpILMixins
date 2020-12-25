using SharpILMixins.Annotations;

namespace SampleProjectCore.Mixins
{
    [Mixin(typeof(SuperClass))]
    public class MixinSuperClass
    {
        [Shadow] private int _myValue;
    }
}