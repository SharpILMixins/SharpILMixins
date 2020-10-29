using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using NLog;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl;
using SharpILMixins.Processor.Workspace.Processor.Scaffolding;

namespace SharpILMixins.Processor.Workspace.Processor
{
    public class MixinProcessor
    {
        public MixinProcessor(MixinWorkspace workspace)
        {
            Workspace = workspace;
            CopyScaffoldingHandler = new CopyScaffoldingHandler(workspace);
        }

        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(MixinProcessor));

        public MixinWorkspace Workspace { get; }

        public CopyScaffoldingHandler CopyScaffoldingHandler { get; set; }

        public RedirectManager RedirectManager => CopyScaffoldingHandler.RedirectManager;

        public void Process(List<MixinRelation> mixinRelations, MixinTargetModule targetModule)
        {
            DumpRequestedTargets(mixinRelations, Workspace.Settings.DumpTargets);

            CopyScaffoldingHandler.CopyNonMixinClasses(Workspace.MixinModule, targetModule.ModuleDef);
            foreach (var mixinRelation in mixinRelations)
            {
                Logger.Info($"Starting to process mixin {mixinRelation.MixinType.Name}");

                if (mixinRelation.IsAccessor)
                {
                    Logger.Info(
                        $"Mixin {mixinRelation.MixinType.Name} is an accessor for {mixinRelation.TargetType.Name}.");
                    RedirectManager.RegisterTypeRedirect(mixinRelation.MixinType, mixinRelation.TargetType);
                    continue;
                }

                CopyScaffoldingHandler.ProcessType(mixinRelation.TargetType, mixinRelation.MixinType);

                foreach (var action in mixinRelation.MixinActions.OrderBy(a => a.Priority))
                {
                    action.LocateTargetMethod();
                    Logger.Debug($"Starting to proccess action for \"{action.MixinMethod.FullName}\"");

                    try
                    {
                        action.CheckIsValid();
                    }
                    catch (Exception e)
                    {
                        throw new MixinApplyException(
                            $"Method \"{action.TargetMethod}\" is not a valid target for \"{action.MixinMethod}\"",
                            e);
                    }

                    var processor =
                        BaseMixinActionProcessorManager.GetProcessor(action.MixinAttribute.GetType(), Workspace);
                    processor.ProcessAction(action, action.MixinAttribute);
                    if (action.TargetMethod.Body != null)
                    {
                        action.TargetMethod.Body.UpdateInstructionOffsets();

                        RedirectManager.ProcessRedirects(action.TargetMethod, action.TargetMethod.Body);
                    }

                    Logger.Debug($"Finished to proccess action for \"{action.MixinMethod.FullName}\"");
                }

                Logger.Info($"Finished to process mixin {mixinRelation.MixinType.Name}");
            }
        }

        private void DumpRequestedTargets(List<MixinRelation> mixinRelations, DumpTargetType dumpTargets)
        {
            foreach (var relation in mixinRelations.DistinctBy(r => r.MixinType.FullName))
            {
                if (!ShouldDump(relation, dumpTargets)) continue;

                var targetType = relation.TargetType;
                Logger.Info($"Target dump for \"{targetType.FullName}\":");

                Logger.Info("Methods:");
                foreach (var method in targetType.Methods)
                {
                    Logger.Info($">> {method.FullName}");

                    if (method.Body != null)
                    {
                        DumpInvokeTargets(method, dumpTargets);
                        DumpFieldTargets(method, dumpTargets);
                    }
                }

                Logger.Info("");
            }
        }

        private void DumpInvokeTargets(MethodDef method, DumpTargetType dumpTargets)
        {
            if (dumpTargets.HasFlagFast(DumpTargetType.Invoke))
            {
                Logger.Info("");
                Logger.Info("Invoke targets:");

                var invokeCalls = method.Body.Instructions
                    .Where(i => InvokeInjectionProcessor.IsCallOpCode(i.OpCode))
                    .Select(i => i.Operand)
                    .OfType<IMethodDefOrRef>()
                    .Select(i => i.FullName).Distinct().ToList();
                invokeCalls.ForEach(c => Logger.Info($">>> {c}"));
            }
        }

        private void DumpFieldTargets(MethodDef method, DumpTargetType dumpTargets)
        {
            if (dumpTargets.HasFlagFast(DumpTargetType.Invoke))
            {
                Logger.Info("");
                Logger.Info("Field targets:");

                var fieldCalls = method.Body.Instructions
                    .Where(i => FieldInjectionProcessor.IsFieldOpCode(i.OpCode))
                    .Select(i => i.Operand)
                    .OfType<IField>()
                    .Select(i => i.FullName).Distinct().ToList();
                fieldCalls.ForEach(c => Logger.Info($">>> {c}"));
            }
        }

        private bool ShouldDump(MixinRelation relation, DumpTargetType dumpTargets)
        {
            return !dumpTargets.HasFlagFast(DumpTargetType.None);
        }
    }
}