using System;

namespace SharpILMixins.Annotations.Inline
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodInlineOptionAttribute : Attribute
    {
        public MethodInlineOptionAttribute(InlineSetting setting)
        {
            Setting = setting;
        }

        public InlineSetting Setting { get; }
    }
}