using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public abstract class BaseMixinAttribute : Attribute
    {
        protected BaseMixinAttribute()
        {
        }

        protected BaseMixinAttribute(string target, int priority = 1000)
        {
            Target = target;
            Priority = priority;
        }

        public string Target { get; set; } = string.Empty;
        public int Priority { get; set; } = 1000;
    }
}