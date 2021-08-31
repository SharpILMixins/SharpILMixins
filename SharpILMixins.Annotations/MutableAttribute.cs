using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MutableAttribute : Attribute
    {
    }
}