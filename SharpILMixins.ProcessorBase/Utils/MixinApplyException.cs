using System;

namespace SharpILMixins.Processor.Utils
{
    public class MixinApplyException : InvalidOperationException
    {
        public MixinApplyException(string? message) : base(message)
        {
        }

        public MixinApplyException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}