using System;
using SampleProjectCore;

namespace SampleProject
{
    public class Program
    {
        private static readonly int _coolNumber = 9;
        private static readonly int[,] COOL_NUMBER_2D_ARRAY = new int[10,10];
        private static readonly RandomEnum[,] COOL_RANDOM_ARRAY = new RandomEnum[10,10];
        private static readonly RandomClass[,] COOL_RANDOM2_ARRAY = new RandomClass[10,10];
        
        private static void Main(string[] args)
        {
            Console.WriteLine($"Hello World from SampleProject {_coolNumber}!");
            var val = BooleanOverload(false);
            if (val) Console.WriteLine("HEY FROM OVERLOAD");
        }

        private static bool BooleanOverload(bool overload)
        {
            return true.Equals(overload);
        }

        private void RandomMethod(bool overload, int randomInt, RandomEnum randomEnum, float floaty, int moreInts)
        {
            var value = COOL_NUMBER_2D_ARRAY[5, 3];
            var value2 = COOL_RANDOM_ARRAY[5, 3];
            var value3 = COOL_RANDOM2_ARRAY[5, 3];
            Console.WriteLine(value);
        }


        private static int RandomNumber()
        {
            return 4;
        }
    }
}