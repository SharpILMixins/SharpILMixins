using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Annotations.Parameters;

namespace SampleProjectCore.Mixins
{
//Target with typeof or full name
[Mixin(typeof(SomeGameClass))]
public class MixinSomeGameClass
{
    //Since these methods will be copied to the target class above,
    //Shadowing will make it so you can access them
    //One can also extend SomeGameClass instead and use them directly
    [Shadow] public bool isRunning;
    [Shadow] public int counter;


    //Method name can be anything you want
    [Inject(At = AtLocation.Head, Target = "DoSomething")]
    private int PrefixDoSomething([InjectCancelParam] out bool cancel)
    {
        isRunning = true;
        if (counter > 100)
        {
            cancel = false;
        }
        counter = 0;
        cancel = true;
        
        return counter * 10;
    }
}
}