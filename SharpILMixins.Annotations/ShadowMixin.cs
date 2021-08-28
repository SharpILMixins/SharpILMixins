using System;
using JetBrains.Annotations;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, Inherited = false)]
    public sealed class ShadowAttribute : Attribute
    {
        public ShadowAttribute()
        {
            
        }

        public ShadowAttribute([CanBeNull] string name)
        {
            Name = name;
        }

        [CanBeNull] public string Name { get; set; } = null;
    }
}