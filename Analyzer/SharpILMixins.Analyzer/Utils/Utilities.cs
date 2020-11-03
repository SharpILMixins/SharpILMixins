namespace SharpILMixins.Analyzer.Utils
{
    public static class Utilities
    {
        public static string GetMixinCode(int code)
        {
            return $"MIXN{code.ToString().PadLeft(3, '0')}";
        }
    }
}