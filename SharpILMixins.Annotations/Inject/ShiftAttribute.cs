using System;

namespace SharpILMixins.Annotations.Inject
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ShiftAttribute : Attribute
    {
        public int ByAmount { get; }

        public ShiftAttribute(int byAmount)
        {
            ByAmount = byAmount;
        }
    }}