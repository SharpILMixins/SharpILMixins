using System;
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
using SharpILMixins.Processor.Workspace.Processor.Scaffolding;
using SharpILMixins.Processor.Workspace.Processor.Scaffolding.Redirects;

namespace SharpILMixins.Processor.Workspace
{
    public class MixinWorkspace : IDisposable
    {
        public MixinWorkspace(FileInfo mixinToApply, DirectoryInfo targetDir, MixinWorkspaceSettings settings)
        {
            ModuleContext = ModuleDef.CreateModuleContext();
            SetupContext();
            TargetDir = targetDir;

            Settings = settings;
            MixinModule = ModuleDefMD.Load(mixinToApply.FullName,
                new ModuleCreationOptions(ModuleContext) {TryToLoadPdbFromDisk = true});
            MixinAssembly = MixinModule.Assembly;

            Configuration = TryToLoadConfiguration(MixinAssembly);
            Loader = new MixinWorkspaceLoader(this);
            MixinProcessor = new MixinProcessor(this);
            PlaceholderManager = new PlaceholderManager(this);
        }

        public MixinWorkspaceSettings Settings { get; }

        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(MixinWorkspace));

        public DirectoryInfo TargetDir { get; }

        public ModuleDefMD MixinModule { get; }

        public AssemblyDef MixinAssembly { get; }

        public ModuleContext ModuleContext { get; set; }

        public PlaceholderManager PlaceholderManager { get; set; }

        public MixinConfiguration Configuration { get; }

        public MixinWorkspaceLoader Loader { get; }

        public MixinProcessor MixinProcessor { get; }

        public RedirectManager RedirectManager => MixinProcessor.CopyScaffoldingHandler.RedirectManager;

        public void Dispose()
        {
            MixinModule.Dispose();
        }

        private void SetupContext()
        {
            var moduleDefMd = ModuleDefMD.Load(typeof(BaseMixinAttribute).Assembly.Location, ModuleContext);
            var assemblyDef = ModuleContext.AssemblyResolver.Resolve(moduleDefMd.Assembly, moduleDefMd);
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
                throw new MixinApplyException("Invalid Mixin project configuration.");

            return jObject.ToObject<MixinConfiguration>() ??
                   throw new MixinApplyException("Unable to load Mixin Configuration correctly.");
        }

        public void Apply()
        {
            var targets = Loader.LocateAndLoadTargets();
            foreach (var targetModule in targets)
            {
                if (targetModule == null) continue;

                var targetModuleModuleDef = targetModule.ModuleDef;
                var targetAssembly = targetModuleModuleDef.Assembly;

                //Add target assembly to cache.
                var assemblyResolver = ModuleContext.AssemblyResolver as AssemblyResolver;
                assemblyResolver?.AddToCache(targetAssembly);

                Logger.Debug($"Starting to process {targetAssembly.FullName}");
                var mixinRelations = Loader.LoadMixins(MixinAssembly, targetAssembly);
                MixinProcessor.Process(mixinRelations, targetModule);

                var filePathFullName = targetModule.FilePath.FullName;
                var finalPath = Path.Combine(Settings.OutputPath,
                    Path.GetFileNameWithoutExtension(filePathFullName) + "-out" + Path.GetExtension(filePathFullName));

                WriteFinalModule(targetModuleModuleDef, finalPath);
                targetModuleModuleDef.Dispose();
                Logger.Debug(
                    $"Finished to process {targetAssembly.FullName} with output named {Path.GetFileName(finalPath)}");
            }
        }

        private static void WriteFinalModule(ModuleDefMD targetModuleModuleDef, string path)
        {
            if (targetModuleModuleDef.IsILOnly)
                targetModuleModuleDef.Write(path, new ModuleWriterOptions(targetModuleModuleDef)
                {
                    WritePdb = true
                });
            else
                targetModuleModuleDef.NativeWrite(path);
        }
    }
}