using System.ComponentModel;
using Newtonsoft.Json;

namespace SharpILMixins.Processor.Workspace
{
    public class MixinConfiguration
    {
        [Description("The default namespace to target the Mixins.")]
        [JsonProperty("namespace")]
        public string? BaseNamespace { get; set; } = "";

        /// <summary>
        /// The Assembly names to target with the Mixins
        /// </summary>
        [Description("The assembly files to target")]
        [JsonProperty("targets")]
        public string[] Targets { get; set; } = new string[0];

        /// <summary>
        /// The mixin classes to apply
        /// </summary>
        [Description("The mixin classes to apply to the targets")]
        [JsonProperty("mixins")]
        public string[] Mixins { get; set; } = new string[0];
    }
}