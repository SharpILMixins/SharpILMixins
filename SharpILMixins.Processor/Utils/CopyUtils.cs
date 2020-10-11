using System.Diagnostics;
using dnlib.DotNet;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Processor.Utils
{
    public static class CopyUtils
    {
        public static MethodDefUser CopyMethod(MethodDef original, MixinWorkspace workspace,
            TypeDef? declaringType = null, bool copyAttributes = true)
        {
            var redirectManager = workspace.MixinProcessor.CopyScaffoldingHandler.RedirectManager;
            var copyMethod = new MethodDefUser(original.Name, original.MethodSig, original.ImplAttributes, original.Attributes)
            {
                Body = original.Body,
            };
            copyMethod.ReturnType = redirectManager.ProcessTypeRedirect(copyMethod.ReturnType);
            foreach (var parameter in copyMethod.Parameters)
            {
                parameter.Type = redirectManager.ProcessTypeRedirect(parameter.Type);
            }
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