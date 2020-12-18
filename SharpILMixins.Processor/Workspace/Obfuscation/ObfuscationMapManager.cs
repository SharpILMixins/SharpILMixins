using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using dnlib.DotNet;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Scaffolding.Redirects;

namespace SharpILMixins.Processor.Workspace.Obfuscation
{
    public class ObfuscationMapManager
    {
        public ObfuscationMapManager(MixinWorkspace workspace, RedirectManager redirectManager)
        {
            Workspace = workspace;
            RedirectManager = redirectManager;
        }

        public MixinWorkspace Workspace { get; }

        public RedirectManager RedirectManager { get; }

        public List<ObfuscationMap> LoadedMaps { get; } = new();

        public void LoadObfuscationMap(ObfuscationMap map)
        {
            LoadedMaps.Add(map);
        }

        public ObfuscationMap CreateUnifiedMap()
        {
            return new(LoadedMaps.SelectMany(c => c.Entries).ToImmutableArray());
        }

        public ObfuscationMap PerformNameRemapping()
        {
            return new(LoadedMaps.SelectMany(m => PerformNameRemapping(m).Entries).ToImmutableArray());
        }

        public ObfuscationMap PerformNameRemapping(ObfuscationMap map)
        {
            var deObfuscationMapEntries = new List<ObfuscationMapEntry>();
            var module = Workspace.CurrentTargetModule ??
                         throw new MixinApplyException(
                             "Unable to perform name remapping because there is no module being processed currently");

            var typeDefs = module.GetTypes().ToList();

            var typeEntries = map.GetEntriesForType(ObfuscationMapEntryType.Type)
                .ToDictionary(c => c.TargetMember,
                    c => (c.DeObfuscatedName, map.Entries.Where(subEntry => c.ParentMember == subEntry.TargetMember)));

            foreach (var typeDef in typeDefs)
            {
                if (!typeEntries.TryGetValue(typeDef.FullName, out var value)) continue;
                var (deobfName, subEntries) = value;
                var oldName = typeDef.Name;

                typeDef.Name = deobfName;
                deObfuscationMapEntries.Add(new ObfuscationMapEntry(ObfuscationMapEntryType.Type, typeDef.FullName,
                    oldName));

                RemapSubEntries(typeDef, subEntries, deObfuscationMapEntries, oldName);
            }

            return new ObfuscationMap(ImmutableArray.Create(deObfuscationMapEntries.ToArray()));
        }

        private static void RemapSubEntries(TypeDef typeDef, IEnumerable<ObfuscationMapEntry> subEntries,
            ICollection<ObfuscationMapEntry> deObfuscationMapEntries,
            UTF8String typeName)
        {
            var memberDefs = typeDef.Fields.Cast<IMemberDef>().Concat(typeDef.Methods).Concat(typeDef.Properties)
                .ToList();
            var subEntriesDict = subEntries.ToList().ToDictionary(c => c.TargetMember, c => c);
            foreach (var memberDef in memberDefs)
            {
                if (!subEntriesDict.TryGetValue(memberDef.FullName, out var deobfSubEntry))
                    continue;

                var oldSubName = memberDef.Name;

                var type = ObfuscationMapEntryType.Method;
                if (memberDef.IsMethod)
                    type = ObfuscationMapEntryType.Type;
                else if (memberDef.IsField)
                    type = ObfuscationMapEntryType.Field;
                else if (memberDef.IsPropertyDef)
                    type = ObfuscationMapEntryType.Property;

                memberDef.Name = deobfSubEntry.DeObfuscatedName;
                deObfuscationMapEntries.Add(new ObfuscationMapEntry(type, memberDef.Name, oldSubName, typeName));
            }
        }
    }
}