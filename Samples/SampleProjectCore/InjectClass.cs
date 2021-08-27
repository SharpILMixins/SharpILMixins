using System;

namespace SampleProjectCore
{
    public struct ConcerningStruct
    {
        public int bruh;
        public int bruh2;
        public int bruh3;
    }

    public class InjectClass
    {
        private double _d;

        private ConcerningStruct WeirdExample(ConcerningStruct valueDeez)
        {
            return default;
        }

        private void Example()
        {
            // `InjectHead` should target here
            Console.WriteLine("EPIC");
            // `InjectShifted` should target here

            // `InjectField` should target here (twice; one for the get and one for the set)
            // `InjectConstant` should target here
            _d += 0.01;

            // `InjectInvoke` should target here
            // `InjectField` should target here
            if (Math.Abs(new Random().Next() - _d) < double.Epsilon)
            {
                // `InjectReturn` should target here
                Console.WriteLine("In");
                // `InjectShifted` should target here
                return;
            }

            Console.WriteLine("Out");
            // `InjectShifted` should target here

            // `InjectReturn` should target here
            // `InjectTail` should target here
        }
    }
}