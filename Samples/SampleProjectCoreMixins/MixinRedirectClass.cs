using System;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;

namespace SampleProjectCore.Mixins
{
    [Mixin(typeof(RedirectClass))]
    public class MixinRedirectClass
    {
        [Shadow]
        private double _d;
     
        [Shadow("e10")]
        private StringWrapperAccessor _shadowedE10;
        
        [Redirect(Method = RedirectClassTargets.Methods.Example, At = AtLocation.Invoke, Target = RedirectClassTargets.Methods.ExampleInjects.Console_WriteLine_Object)]
        public static void RedirectPrintLn(object value)
        {
            Console.Error.WriteLine(value);
        }

        [Redirect(Method = RedirectClassTargets.Methods.Example, At = AtLocation.NewObj,
            Target = RedirectClassTargets.Methods.ExampleInjects.StringWrapper_ctor_String)]
        public static StringWrapper RedirectStringWrapperCtor(string value)
        {
            return new("mixin'd [wrapper]: " + value);
        }
        
        [Redirect(Method = RedirectClassTargets.Methods.Example, At = AtLocation.Constant, ConstantValue = "EPIC", Ordinal = 1)]
        public static string RedirectString(string value)
        {
            return "mixin'd: " + value;
        }
        
        [Redirect(Method = RedirectClassTargets.Methods.Example, At = AtLocation.Field, Target = RedirectClassTargets.Methods.ExampleInjects._d, Ordinal = 0)]
        public double RedirectFieldGet(double value)
        {
            Console.Error.WriteLine(_shadowedE10);
            return value + 3;
        }
        
        [Redirect(Method = RedirectClassTargets.Methods.Example, At = AtLocation.Field, Target = RedirectClassTargets.Methods.ExampleInjects._d, Ordinal = 1)]
        public void RedirectFieldSet(double value)
        {
            _d = value + 1;
        }
    }
}