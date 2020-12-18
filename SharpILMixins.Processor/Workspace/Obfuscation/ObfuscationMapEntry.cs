using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SharpILMixins.Processor.Workspace.Obfuscation
{
    public record ObfuscationMapEntry(
        [JsonConverter(typeof(StringEnumConverter))]
        ObfuscationMapEntryType Type,
        string TargetMember,
        string DeObfuscatedName,
        string? ParentMember = null);
}