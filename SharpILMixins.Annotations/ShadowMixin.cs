using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false)]
    public sealed class ShadowAttribute : Attribute
    {
    }
}