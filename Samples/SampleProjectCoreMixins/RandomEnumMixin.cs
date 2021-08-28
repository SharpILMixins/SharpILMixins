using SharpILMixins.Annotations;

namespace SampleProjectCore.Mixins
{
    [Mixin(typeof(RandomEnum))]
    public enum RandomEnumMixin
    {
        I = 5,
        Am,
        Fine,
        Thanks,
        For,
        Asking
    }
}