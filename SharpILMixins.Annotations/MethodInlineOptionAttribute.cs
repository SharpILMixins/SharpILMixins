using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodInlineOptionAttribute : Attribute
    {
        public InlineSetting Setting { get; }

        public MethodInlineOptionAttribute(InlineSetting setting)
        {
            Setting = setting;
        }
    }
}