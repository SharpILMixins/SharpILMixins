using System;
using JetBrains.Annotations;
using SharpILMixins.Annotations.Inject;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class RedirectAttribute : BaseMixinAttribute
    {
        public AtLocation At { get; set; }

        public string Target { get; set; }
        
        [CanBeNull] public object ConstantValue { get; set; }

        public int Ordinal { get; set; } = -1;
    }
}