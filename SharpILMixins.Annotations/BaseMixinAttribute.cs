using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public abstract class BaseMixinAttribute : Attribute
    {
        protected BaseMixinAttribute()
        {
        }

        protected BaseMixinAttribute(string method, int priority = 1000)
        {
            Method = method;
            Priority = priority;
        }

        public string Method { get; set; } = string.Empty;
        
        public int Priority { get; set; } = 1000;
    }
}