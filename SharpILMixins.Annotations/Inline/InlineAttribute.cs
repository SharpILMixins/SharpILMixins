namespace SharpILMixins.Annotations.Inline
{
    public class InlineAttribute : MethodInlineOptionAttribute
    {
        public InlineAttribute() : base(InlineSetting.DoInline)
        {
        }
    }
}