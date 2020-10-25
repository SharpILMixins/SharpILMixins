using System;
using SampleProject;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Annotations.Parameters;

namespace SampleProjectCore.Mixins
{
    [Mixin(typeof(Program))]
    public class MixinProgram
    {
        [Shadow] private static int _coolNumber;

        [Unique]
        private static string _ourString = "";

        [Overwrite]
        [MethodTarget(typeof(int), "RandomNumber")]
        public static int RandomNumber()
        {
            return 42;
        }

        [Inject("Main", AtLocation.Head)]
        public static void BeforeMain(string[] args, [InjectCancelParam] out bool isCancelled)
        {
            isCancelled = true;
            Console.WriteLine($"Hello World from Mixins! Random number was: {RandomNumber()} {args}");
            Console.WriteLine($"Cool number before: {_coolNumber}");
            _coolNumber = 42;
            _ourString = "Mixins on C# are cool!";
        }

        [Inject("System.Void SampleProject.Program::Main(System.String[])", AtLocation.Head, 5000)]
        public static void BeforeMainFirst(ref string[] args)
        {
            args[0] = "Bruh";
            Console.WriteLine($"Hello World from Mixins! This mixin has 5000 priority, so it comes first.");
        }

        [Inject("System.Void SampleProject.Program::Main(System.String[])", AtLocation.Return)]
        public static void AfterMain(string[] args)
        {
            Console.WriteLine($"Goodbye World from Mixins!");
            Console.WriteLine($"Cool number after: {_coolNumber}");
            Console.WriteLine($"Truth: \"{_ourString}\"");
            Console.WriteLine("");
            Console.WriteLine($"Also, here are the args from the Main method: {args}");

        }
    }
}