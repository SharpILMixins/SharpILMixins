namespace SampleProjectCore
{
    public class SomeGameClass
    {
        public int counter;
        public bool isRunning;

        private int DoSomething()
        {
            if (isRunning) counter++;
            return counter * 10;
        }
    }
}