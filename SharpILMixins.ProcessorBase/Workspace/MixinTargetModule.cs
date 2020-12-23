using System.IO;
using dnlib.DotNet;

namespace SharpILMixins.Processor.Workspace
{
    public record MixinTargetModule(FileInfo FilePath, ModuleDefMD ModuleDef)
    {
    }
}