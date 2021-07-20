using System.Collections.Generic;

namespace SampleProjectCore
{
    public class SomeGameClass
    {
        public static readonly Dictionary<int, RandomClass> COOL_RANDOM_DICTIONARY = new Dictionary<int, RandomClass>();
        public int counter;
        public bool isRunning;

        private int DoSomething()
        {
            if (isRunning) counter++;
            return counter * 10;
        }
    }
}