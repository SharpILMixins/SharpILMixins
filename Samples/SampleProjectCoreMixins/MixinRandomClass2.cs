using SharpILMixins.Annotations;

namespace SampleProjectCore.Mixins
{
    [Mixin(typeof(RandomClass.RandomClass2))]
    public class MixinRandomClass2 : RandomClass, RandomClass.IRandomInterface
    {
        public double GetDoubleValue()
        {
            return DoubleValue;
        }
    }
}