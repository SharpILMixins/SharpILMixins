using System;
using static System.String;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false)]
    public sealed class ShadowAttribute : Attribute
    {
        public ShadowAttribute()
        {
        }
    }
}