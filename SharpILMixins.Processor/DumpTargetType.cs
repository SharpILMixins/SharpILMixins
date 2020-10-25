using System;

namespace SharpILMixins.Processor
{
    [Flags]
    public enum DumpTargetType
    {
        None = 0,
        All = 1 << 0
    }

    public static class DumpTargetTypeExtensions
    {
        public static bool HasFlagFast(this DumpTargetType value, DumpTargetType flag)
        {
            return (value & flag) != 0;
        }
    }
}