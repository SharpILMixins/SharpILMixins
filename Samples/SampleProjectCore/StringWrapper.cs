namespace SampleProjectCore
{
    internal class StringWrapper
    {
        public StringWrapper(string epic)
        {
            Epic = epic;
        }

        public string Epic { get; }

        public override string ToString()
        {
            return Epic;
        }
    }
}