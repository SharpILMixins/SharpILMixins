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
        public MixinWorkspace Workspace { get; set; }

        public MixinRelation(TypeDef mixinType, TypeDef targetType, MixinWorkspace workspace)
        {
            MixinType = mixinType;
            TargetType = targetType;
            Workspace = workspace;
            MixinActions = LoadActions(mixinType);
        }

        public List<MixinAction> LoadActions(TypeDef mixinType)
        {
            return mixinType.Methods
                .Select(m =>
                {
                    var attribute = m.GetCustomAttribute<BaseMixinAttribute>();
                    return attribute != null ? new MixinAction(m, attribute, TargetType, Workspace) : null;
                }).Where(t => t is not null).ToList()!;
        }

        public List<MixinAction> MixinActions { get; set; }
    }
}