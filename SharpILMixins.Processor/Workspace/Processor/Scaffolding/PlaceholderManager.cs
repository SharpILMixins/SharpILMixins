using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class PlaceholderManager
    {
        public MixinWorkspace Workspace { get; }

        public PlaceholderManager(MixinWorkspace workspace)
        {
            Workspace = workspace;
        }

        public void RegisterPlaceholder(string key, string value)
        {
            RegisterDynamicPlaceholder(key, () => value);
        }

        public void RegisterDynamicPlaceholder(string key, Func<string> valueFetcher)
        {
            Placeholders.Add(key, valueFetcher);
        }

        public Dictionary<string, Func<string>> Placeholders { get; set; } = new Dictionary<string, Func<string>>();

        public void ProcessPlaceholders(CilBody body)
        {
            foreach (var instruction in body.Instructions)
            {
                if (instruction.Operand is string str)
                {
                    foreach (var (key, func) in Placeholders)
                    {
                        str = str.Replace($"$${key}$$", func());
                    }

                    instruction.Operand = str;
                }
            }
        }
    }
}