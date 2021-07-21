using System;
using System.Diagnostics;
using NBrigadier;
using NBrigadier.Arguments;
using NBrigadier.Builder;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Annotations.Inline;
using SharpILMixins.Annotations.Parameters;

namespace SampleProjectCore.Mixins
{
    [Mixin("SampleProject.Program")]
    public class MixinProgram
    {

        public static void Method<T>(Predicate<T> a)
        {

        }
        
        [Shadow] private static int _coolNumber;

        [Unique] private static string _ourString = "";

        [Overwrite]
        [NoInline]
        [MethodTarget(typeof(int), "RandomNumber")]
        public static int RandomNumber()
        {
            return 42;
        }

        [Inject(Method = ProgramBruhTargets.Methods.RandomMethod, At = AtLocation.Head)]
        public void RandomMethod(bool overload, int randomInt, RandomEnum randomEnum, float floaty, int moreInts)
        {
            Console.WriteLine($"{overload}, {randomInt}, {randomEnum}, {floaty.ToString()}, {moreInts}, {new object().ToString()}");
        }

        //[Inject(Method = "Main", At = AtLocation.Head)]
        public static void BeforeMain(string[] args, [InjectCancelParam] out bool isCancelled)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, __) => null;
            Debugger.Launch();
            Debugger.Break();
            isCancelled = true;
            Console.WriteLine($"Hello World from Mixins! Random number was: {RandomNumber()} {args}");
            Console.WriteLine($"Cool number before: {_coolNumber}");
            _coolNumber = 42;
            _ourString = "Mixins on C# are cool!";
        }

        [Inject(ProgramBruhTargets.Methods.Main, AtLocation.Head, 5000)]
        public static void BeforeMainFirst(ref string[] args)
        {
            args[0] = "Bruh";
            Console.WriteLine("Hello World from Mixins! This mixin has 5000 priority, so it comes first.");
        }

        [Inject(ProgramBruhTargets.Methods.Main, AtLocation.Return)]
        public static void AfterMain(string[] args)
        {
            Console.WriteLine("Goodbye World from Mixins!");
            Console.WriteLine($"Cool number after: {_coolNumber}");
            Console.WriteLine($"Truth: \"{_ourString}\"");
            Console.WriteLine("");
            Console.WriteLine($"Also, here are the args from the Main method: {args}");
            Method<object>(a => a != null);
            
            Console.WriteLine($"");


            var dispatcher = new CommandDispatcher<RandomClass>();

            dispatcher.Register(
                LiteralArgumentBuilder<RandomClass>.Literal("foo")
                    .Then(
                        RequiredArgumentBuilder<RandomClass, int>.Argument("bar", IntegerArgumentType.Integer())
                            .Executes(c =>
                            {
                                Console.WriteLine("Bar is " + IntegerArgumentType.GetInteger(c, "bar"));
                                return 1;
                            })
                    )
                    .Executes(c =>
                    {
                        Console.WriteLine("Called foo with no arguments");
                        return 1;
                    })
            );
            var dispatcher2 = new CommandDispatcher<Source>();

            dispatcher2.Register(
                LiteralArgumentBuilder<Source>.Literal("foo")
                    .Then(
                        RequiredArgumentBuilder<Source, int>.Argument("bar", IntegerArgumentType.Integer())
                            .Executes(c =>
                            {
                                Console.WriteLine("Bar is " + IntegerArgumentType.GetInteger(c, "bar"));
                                return 1;
                            })
                    )
                    .Executes(c =>
                    {
                        Console.WriteLine("Called foo with no arguments");
                        return 1;
                    })
            );

        }
    }
}