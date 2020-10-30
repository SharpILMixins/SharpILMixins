namespace SharpILMixins.LoggerAbstraction
{
    public static class Utilities
    {
        public static bool DebugMode { get; set; } =

#if DEBUG
            true
#else
            false
#endif
            ;
    }
}