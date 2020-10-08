using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NLog;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor;

namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspace
    {
        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(MixinWorkspace));

        public DirectoryInfo TargetDir { get; }
        public bool ShouldDumpTargets { get; }

        public ModuleDefMD MixinModule { get; }

        public AssemblyDef MixinAssembly { get; }

        public ModuleContext ModuleContext { get; set; }

        public MixinWorkspace(FileInfo mixinToApply, DirectoryInfo targetDir, bool shouldDumpTargets)
        {
            ModuleContext = ModuleDef.CreateModuleContext();
            SetupContext();
            TargetDir = targetDir;
            ShouldDumpTargets = shouldDumpTargets;

            MixinModule = ModuleDefMD.Load(mixinToApply.FullName, ModuleContext);
            MixinAssembly = MixinModule.Assembly;

            Configuration = TryToLoadConfiguration(MixinAssembly);
            Loader = new MixinWorkspaceLoader(this);
            MixinProcessor = new MixinProcessor(this);
        }

        private void SetupContext()
        {
            ModuleDefMD.Load(typeof(BaseMixinAttribute).Assembly.Location, ModuleContext);
        }

        public static MixinConfiguration TryToLoadConfiguration(AssemblyDef mixinToApply)
        {
            var mixinConfigurationResource = Utilities.ReadResource(mixinToApply, "mixins.json") ??
                                             throw new MixinApplyException(
                                                 "Unable to find mixins.json on the provided Assembly file.");

            var mixinConfigEmbeddedResource = mixinConfigurationResource as EmbeddedResource ??
                                              throw new MixinApplyException(
                                                  $"Found mixins configuration file \"{mixinConfigurationResource.Name}\", but it is not an embedded resource.");

            using StreamReader reader = new StreamReader(mixinConfigEmbeddedResource.CreateReader().AsStream());

            var configurationString = reader.ReadToEnd();

            var jSchema = JSchema.Parse(Utilities.ReadResource("mixin.config.schema.json"));
            if (!(JsonConvert.DeserializeObject(configurationString) is JObject jObject) || !jObject.IsValid(jSchema))
            {
                throw new MixinApplyException("Invalid Mixin project configuration.");
            }

            return jObject.ToObject<MixinConfiguration>() ??
                   throw new MixinApplyException("Unable to load Mixin Configuration correctly.");
        }

        public MixinConfiguration Configuration { get; }

        public MixinWorkspaceLoader Loader { get; }

        public MixinProcessor MixinProcessor { get; }

        public void Apply()
        {
            var targets = Loader.LocateAndLoadTargets();
            foreach (var targetModule in targets)
            {
                if (targetModule == null) continue;

                var targetModuleModuleDef = targetModule.ModuleDef;
                var targetAssembly = targetModuleModuleDef.Assembly;

                Logger.Debug($"Starting to process {targetAssembly.FullName}");
                var mixinRelations = Loader.LoadMixins(MixinAssembly, targetAssembly);
                MixinProcessor.Process(mixinRelations, targetModule);

                var filePathFullName = targetModule.FilePath.FullName;
                var finalPath = Path.GetFileNameWithoutExtension(filePathFullName) + "-out" +
                                 Path.GetExtension(filePathFullName);
                
                WriteFinalModule(targetModuleModuleDef, finalPath);
                Logger.Debug($"Finished to process {targetAssembly.FullName} with output named {Path.GetFileName(finalPath)}");
            }
        }

        private static void WriteFinalModule(ModuleDefMD targetModuleModuleDef, string path)
        {
            if (targetModuleModuleDef.IsILOnly)
            {
                targetModuleModuleDef.Write(path);
            }
            else
            {
                targetModuleModuleDef.NativeWrite(path);
            }
        }
    }
}