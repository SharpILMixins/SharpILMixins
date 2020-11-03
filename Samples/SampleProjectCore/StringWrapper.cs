namespace SampleProjectCore
{
    class StringWrapper
    {
        public string Epic { get; }

        public StringWrapper(string epic) => Epic = epic;

        public override string ToString() => Epic;
    }
}