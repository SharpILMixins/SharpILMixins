using System;

namespace SharpILMixins.Annotations.Parameters
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class InjectCancelParamAttribute : BaseParameterAttribute
    {
        public InjectCancelParamAttribute()
        {
        }
    }
}