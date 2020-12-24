using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Processor
{
    internal class Program
    {
        public static Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(Program));

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ProcessOptions, GenerateOptions>(args)
                .WithParsed<GenerateOptions>(o => Execute(new ProcessOptions
                {
                    MixinsToApply = o.MixinsToApply,
                    OutputDir = o.OutputDir,
                    TargetDir = o.TargetDir,
                    DeObfuscationMapsToApply = o.DeObfuscationMapsToApply,
                    PauseOnExit = o.PauseOnExit,
                    GenerationType = GenerationType.HelperCode
                })).WithParsed<GenerateMappedOptions>(o =>
                {
                    if (o.DeObfuscationMapsToApply?.Any() != true)
                    {
                        throw new MixinApplyException(
                            "In order to generate a mapped assembly, there needs to be at least one de-obfuscation map supplied.");
                    }
                    
                    Execute(new ProcessOptions
                    {
                        MixinsToApply = o.MixinsToApply,
                        OutputDir = o.OutputDir,
                        TargetDir = o.TargetDir,
                        DeObfuscationMapsToApply = o.DeObfuscationMapsToApply,
                        PauseOnExit = o.PauseOnExit,
                        GenerationType = GenerationType.HelperCode
                    });
                })
                .WithParsed<ProcessOptions>(Execute);
        }

        private static void Execute(ProcessOptions o)
        {
            try
            {
                ProcessMixins(o);
            }
            catch (Exception e)
            {
                LogException(e);
                Environment.ExitCode = 1;
            }

            if (!o.PauseOnExit) return;
            Logger.Info("Press any key to continue..");
            Console.ReadKey();
        }

        private static void LogException(Exception e, bool inner = false)
        {
            string? diagnosticMessage = null;
            if (e is MixinApplyException || !Utilities.DebugMode)
            {
                diagnosticMessage = inner ? string.Empty : "An error occurred while applying a mixin: ";
                diagnosticMessage += e.Message;
            }

            if (diagnosticMessage == null)
                Logger.Fatal(e);
            else
                Logger.Fatal(e, diagnosticMessage);

            if (e.InnerException != null && e.InnerException != e) LogException(e.InnerException, true);
        }

        private static void ProcessMixins(ProcessOptions o)
        {
            foreach (var mixinAssemblyFile in o.MixinsToApply)
            {
                Logger.Info($"Starting to process {mixinAssemblyFile.Name}");
                try
                {
                    var workDir = new DirectoryInfo(Environment.CurrentDirectory);

                    var outputSuffix = o.OutputSuffix;
                    if (o.GenerationType == GenerationType.Mapped && string.IsNullOrEmpty(outputSuffix))
                        outputSuffix = "mapped";

                    var workspace = new MixinWorkspace(mixinAssemblyFile,
                        o.TargetDir ?? workDir,
                        new MixinWorkspaceSettings((o.OutputDir ?? workDir).FullName,
                            o.DumpTargets,
                            o.MixinHandlerName,
                            o.ExperimentalCopyResources,
                            o.ExperimentalInlineMethods,
                            outputSuffix,
                            o.GenerationType));


                    Debug.Assert(o.DeObfuscationMapsToApply != null, "o.DeObfuscationMapsToApply != null");
                    foreach (var fileInfo in o.DeObfuscationMapsToApply) workspace.AddDeObfuscationMap(fileInfo);

                    workspace.Apply();
                }
                catch (Exception e)
                {
                    throw new MixinApplyException(
                        $"{mixinAssemblyFile.Name} is not a valid Mixin workspace",
                        e);
                }
            }
        }

        public class BaseTargetOptions
        {
            [Option('t', "target-dir", Required = true, HelpText = "The directory of the target assemblies")]
            public DirectoryInfo? TargetDir { get; set; }

            [Option('o', "output-dir",
                HelpText = "The directory of where to place the output files processed by this tool")]
            public DirectoryInfo? OutputDir { get; set; }

            [Option('m', "mixins", Required = true, HelpText = "The path to the Mixin Assemblies to apply")]
            public IEnumerable<FileInfo> MixinsToApply { get; set; } = null!;

            [Option("obf-map", HelpText = "The de-obfuscation maps to apply while applying the Mixins")]
            public IEnumerable<FileInfo>? DeObfuscationMapsToApply { get; set; }

            [Option('p', "pause",
                HelpText = "Whether or not to wait for the user's input after the processing is done")]
            public bool PauseOnExit { get; set; }
        }

        [Verb("generate", aliases: new[] {"g"}, HelpText = "Generate helper code to work with Mixins")]
        public class GenerateOptions : BaseTargetOptions
        {
        }

        [Verb("generate-mapped", aliases: new[] {"gm"},
            HelpText = "Generate mapped target module to make working with Mixins easier")]
        public class GenerateMappedOptions : BaseTargetOptions
        {
        }

        [Verb("process", true, new[] {"p"}, HelpText = "Apply Mixins made with SharpILMixins")]
        public class ProcessOptions : BaseTargetOptions
        {
            [Option('d', "dump-targets", HelpText = "Whether or not dump the targets to the console output")]
            public DumpTargetType DumpTargets { get; set; } = DumpTargetType.None;

            [Option("experimental-inline-methods", HelpText = "Whether or not to inline methods [Experimental]")]
            public bool ExperimentalInlineMethods { get; set; }

            [Option("mixin-handler-name", HelpText = "Prefix for the unique name of the handler methods")]
            public string MixinHandlerName { get; set; } = "mixin";

            [Option("out-suffix", HelpText = "The output suffix of the input file.")]
            public string OutputSuffix { get; set; } = "";

            public GenerationType GenerationType { get; set; } = GenerationType.None;
            
            [Option("experimental-copy-resources", HelpText = "Whether or not to copy Mixin resources into the target method [Experimental]")]
            public bool ExperimentalCopyResources { get; set; }
        }
    }
}