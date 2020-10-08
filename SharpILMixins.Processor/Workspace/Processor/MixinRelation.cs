using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions;

namespace SharpILMixins.Processor.Workspace.Processor
{
    public record MixinRelation
    {
        public TypeDef MixinType { get; }
        public TypeDef TargetType { get; }

        public MixinRelation(TypeDef mixinType, TypeDef targetType)
        {
            MixinType = mixinType;
            TargetType = targetType;
            MixinActions = LoadActions(mixinType);
        }

        public List<MixinAction> LoadActions(TypeDef mixinType)
        {
            return mixinType.Methods
                .Select(m =>
                {
                    var attribute = m.GetCustomAttribute<BaseMixinAttribute>();
                    return attribute != null ? new MixinAction(m, attribute, TargetType) : null;
                }).Where(t => t is not null).ToList()!;
        }

        public List<MixinAction> MixinActions { get; set; }
    }
}