using System;

namespace SampleProjectCore
{
    public class InjectClass
    {
        
        private double d = 0;

        private void example() {
            // `injectHead` should target here
            Console.WriteLine("EPIC");
            // 'injectShifted' should target here

            // `injectField` should target here (twice; one for the get and one for the set)
            // `injectConstant` should target here
            d += 0.01;

            // `injectInvoke` should target here
            // `injectField` should target here
            if (Math.Abs(new Random().Next() - d) < double.Epsilon) {
                // `injectReturn` should target here
                return;
            }

            // `injectReturn` should target here
            // `injectTail` should target here
        }
    }
}