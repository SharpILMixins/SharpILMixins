using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class PlaceholderManager
    {
        public PlaceholderManager(MixinWorkspace workspace)
        {
            Workspace = workspace;
        }

        public MixinWorkspace Workspace { get; }

        public Dictionary<string, Func<string>> Placeholders { get; set; } = new Dictionary<string, Func<string>>();

        public void RegisterPlaceholder(string key, string value)
        {
            RegisterDynamicPlaceholder(key, () => value);
        }

        public void RegisterDynamicPlaceholder(string key, Func<string> valueFetcher)
        {
            Placeholders.Add(key, valueFetcher);
        }

        public void ProcessPlaceholders(CilBody body)
        {
            foreach (var instruction in body.Instructions)
                if (instruction.Operand is string str)
                {
                    foreach (var (key, func) in Placeholders) str = str.Replace($"$${key}$$", func());

                    instruction.Operand = str;
                }
                else if (instruction.Operand is UTF8String utf8String)
                {
                    var tmpStr = utf8String.String;
                    foreach (var (key, func) in Placeholders) tmpStr = tmpStr.Replace($"$${key}$$", func());

                    instruction.Operand = new UTF8String(tmpStr);
                }
        }
    }
}