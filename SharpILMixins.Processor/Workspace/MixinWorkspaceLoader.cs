﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using NLog;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor;

namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspaceLoader
    {
        public MixinWorkspaceLoader(MixinWorkspace workspace)
        {
            Workspace = workspace;
            Configuration = workspace.Configuration;
        }

        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(MixinWorkspaceLoader));

        public MixinWorkspace Workspace { get; }

        public MixinConfiguration Configuration { get; }

        public List<MixinTargetModule> LocateAndLoadTargets()
        {
            return Configuration.Targets
                .Select(LocateTarget)
                .Select(s =>
                {
                    var moduleDef = ModuleDefMD.Load(s,
                        new ModuleCreationOptions(Workspace.ModuleContext) {TryToLoadPdbFromDisk = true});
                    if (moduleDef.PdbState == null)
                        moduleDef.SetPdbState(new PdbState(moduleDef, PdbFileKind.PortablePDB));
                    return new MixinTargetModule(new FileInfo(s),
                        moduleDef);
                })
                .ToList();
        }

        /// <summary>
        ///     Locates a target by looking it up on the configuration file
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string LocateTarget(string name)
        {
            Logger.Debug($"Attempting to locate target named \"{name}\"");
            var result = Workspace.TargetDir.EnumerateFiles(name)
                             .FirstOrDefault(c => !Path.GetFileNameWithoutExtension(c.FullName).EndsWith("-out"))
                             ?.FullName ??
                         throw new MixinApplyException($"Unable to find target named {name}");

            Logger.Debug($"Target named \"{name}\" was found.");
            return result;
        }

        public List<MixinRelation> LoadMixins(AssemblyDef mixinAssembly, AssemblyDef targetAssembly)
        {
            return Configuration.Mixins.Select(s => LoadMixin(s, mixinAssembly, targetAssembly)).ToList()!;
        }

        private MixinRelation LoadMixin(string typeName, AssemblyDef mixinAssembly,
            AssemblyDef targetAssembly)
        {
            var mixinTypes = mixinAssembly.Modules.SelectMany(c => c.Types).ToDictionary(t => t.FullName, t => t);
            var targetTypes = targetAssembly.Modules.SelectMany(c => c.Types)
                .SelectMany(c => c.NestedTypes.Concat(new[] {c})).ToDictionary(t => t.FullName, t => t);
            var fullName = typeName;
            if (!fullName.Contains('.') && Configuration.BaseNamespace != null)
                fullName = $"{Configuration.BaseNamespace}.{fullName}";

            var foundType = mixinTypes.GetValueOrDefault(fullName) ??
                            throw new MixinApplyException(
                                $"Unable to find Mixin type {fullName} in \"{mixinAssembly.Name}\"");

            var attribute = foundType.GetCustomAttribute<MixinAttribute>() ??
                            throw new MixinApplyException(
                                $"Unable to find [Mixin]/[Accessor] Attribute in type {fullName} in \"{mixinAssembly.Name}\"");

            var targetType = targetTypes.GetValueOrDefault(attribute.Target) ?? throw new MixinApplyException(
                $"Unable to find Target Type \"{attribute.Target}\" in Target Assembly");

            return new MixinRelation(foundType, targetType, Workspace, attribute);
        }
    }
}