using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SharpILMixins.Processor.Workspace.Obfuscation
{
    public record ObfuscationMap(ImmutableArray<ObfuscationMapEntry> Entries)
    {
        public IEnumerable<ObfuscationMapEntry> GetEntriesForType(ObfuscationMapEntryType type)
        {
            return Entries.Where(entry => entry.Type == type);
        }
    }
}