using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using NLog;
using NLog.Fluent;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl;
using SharpILMixins.Processor.Workspace.Processor.Scaffolding;

namespace SharpILMixins.Processor.Workspace.Processor
{
    public class MixinProcessor
    {
        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(MixinProcessor));

        public MixinWorkspace Workspace { get; }

        public CopyScaffoldingHandler CopyScaffoldingHandler { get; set; }

        public MixinProcessor(MixinWorkspace workspace)
        {
            Workspace = workspace;
            CopyScaffoldingHandler = new CopyScaffoldingHandler(workspace);
        }

        public void Process(List<MixinRelation> mixinRelations, MixinTargetModule targetModule)
        {
            if (Workspace.Settings.ShouldDumpTargets) DumpTargetsIfRequired(mixinRelations);

            CopyScaffoldingHandler.CopyNonMixinClasses(Workspace.MixinModule, targetModule.ModuleDef);
            foreach (var mixinRelation in mixinRelations)
            {
                Logger.Info($"Starting to process mixin {mixinRelation.MixinType.Name}");
                
                CopyScaffoldingHandler.ProcessType(mixinRelation.TargetType, mixinRelation.MixinType);

                foreach (var action in mixinRelation.MixinActions.OrderBy(a => a.Priority))
                {
                    Logger.Debug($"Starting to proccess action for \"{action.MixinMethod.FullName}\"");

                    
                    var processor = BaseMixinActionProcessorManager.GetProcessor(action.MixinAttribute.GetType(), Workspace);
                    processor.ProcessAction(action, action.MixinAttribute);
                    
                    CopyScaffoldingHandler.RedirectManager.ProcessRedirects(action.TargetMethod.Body);
                    Logger.Debug($"Finished to proccess action for \"{action.MixinMethod.FullName}\"");
                }

                Logger.Info($"Finished to process mixin {mixinRelation.MixinType.Name}");
            }
            
        }

        private void DumpTargetsIfRequired(List<MixinRelation> mixinRelations)
        {
            foreach (var relation in mixinRelations.DistinctBy(r => r.MixinType.FullName))
            {
                var targetType = relation.TargetType;
                Logger.Info($"> {targetType.FullName}");
                foreach (var method in targetType.Methods)
                {
                    Logger.Info($">> {method.FullName}");
                }
            }
        }
    }
}