using System;

namespace SampleProjectCore
{
    public class InjectClass
    {
        private double _d = 0;

        private void Example() {
            // `InjectHead` should target here
            Console.WriteLine("EPIC");
            // `InjectShifted` should target here

            // `InjectField` should target here (twice; one for the get and one for the set)
            // `InjectConstant` should target here
            _d += 0.01;

            // `InjectInvoke` should target here
            // `InjectField` should target here
            if (Math.Abs(new Random().Next() - _d) < double.Epsilon) {
                // `InjectReturn` should target here
                return;
            }

            // `InjectReturn` should target here
            // `InjectTail` should target here
        }
    }
}