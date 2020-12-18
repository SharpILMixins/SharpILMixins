using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Annotations.Parameters;

namespace SampleProjectCore.Mixins
{
//Target with typeof or full name
    [Mixin(typeof(SomeGameClass))]
    public class MixinSomeGameClass
    {
        [Shadow] public int counter;

        //Since these methods will be copied to the target class above,
        //Shadowing will make it so you can access them
        //One can also extend SomeGameClass instead and use them directly
        [Shadow] public bool isRunning;


        //Method name can be anything you want
        [Inject(At = AtLocation.Head, Method = "DoSomething")]
        private int PrefixDoSomething([InjectCancelParam] out bool cancel)
        {
            isRunning = true;
            if (counter > 100) cancel = false;
            counter = 0;
            cancel = true;

            return counter * 10;
        }
    }
}