namespace SharpILMixins.Annotations.Inject
{
    public enum Shift
    {
        /// <summary>
        ///     Do not shift the returned opcodes
        /// </summary>
        None,

        /// <summary>
        ///     Shift the returned opcodes back one instruction
        /// </summary>
        Before,

        /// <summary>
        ///     Shift the returned opcodes forward one instruction
        /// </summary>
        After,

        /// <summary>
        ///     Shift the returned opcodes by the amount specified in ByAmount
        /// </summary>
        By
    }
}