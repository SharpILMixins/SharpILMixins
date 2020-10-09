using System.Diagnostics;
using dnlib.DotNet;

namespace SharpILMixins.Processor.Utils
{
    public static class CopyUtils
    {
        public static MethodDefUser CopyMethod(MethodDef original, TypeDef? declaringType = null, bool copyAttributes = true)
        {
            var copyMethod = new MethodDefUser(original.Name, original.MethodSig, original.ImplAttributes, original.Attributes)
            {
                Body = original.Body
            };
            copyMethod.ParamDefs.Clear();
            foreach (var originalParamDef in original.ParamDefs)
            {
                copyMethod.ParamDefs.Add(new ParamDefUser(originalParamDef.Name, originalParamDef.Sequence, originalParamDef.Attributes));
            }
            if (declaringType != null) copyMethod.DeclaringType = declaringType;
            if (copyAttributes) copyMethod.Attributes = original.Attributes;
            return copyMethod;
        }
    }
}