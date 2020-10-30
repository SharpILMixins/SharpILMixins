using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using NLog;
using SharpILMixins.LoggerAbstraction;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace;
using Utilities = SharpILMixins.LoggerAbstraction.Utilities;

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
                    IsGenerateOnly = true
                }))
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
                LogException(e, o);
                Environment.ExitCode = 1;
            }

            if (!o.PauseOnExit) return;
            Logger.Info("Press any key to continue..");
            Console.ReadKey();
        }

        private static void LogException(Exception e, ProcessOptions processOptions, bool inner = false)
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

            if (e.InnerException != null && e.InnerException != e) LogException(e.InnerException, processOptions, true);
        }

        private static void ProcessMixins(ProcessOptions o)
        {
            foreach (var mixinAssemblyFile in o.MixinsToApply)
            {
                Logger.Info($"Starting to process {mixinAssemblyFile.Name}");
                try
                {
                    var workDir = new DirectoryInfo(Environment.CurrentDirectory);

                    var workspace = new MixinWorkspace(mixinAssemblyFile, o.TargetDir ?? workDir,
                        new MixinWorkspaceSettings((o.OutputDir ?? workDir).FullName, o.DumpTargets,
                            o.MixinHandlerName, o.ExperimentalInlineMethods, o.IsGenerateOnly));

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
        }

        [Verb("generate", aliases: new[] {"g"}, HelpText = "Generate helper code to work with Mixins")]
        public class GenerateOptions : BaseTargetOptions{}

        [Verb("process", true, new[] {"p"}, HelpText = "Offline process Mixins")]
        public class ProcessOptions : BaseTargetOptions
        {
            [Option('d', "dump-targets", HelpText = "Whether or not dump the targets to the console output")]
            public DumpTargetType DumpTargets { get; set; } = DumpTargetType.None;

            [Option("experimental-inline-methods", HelpText = "Whether or not to inline methods [Experimental]")]
            public bool ExperimentalInlineMethods { get; set; }

            [Option("mixin-handler-name", HelpText = "Prefix for the unique name of the handler methods")]
            public string MixinHandlerName { get; set; } = "mixin";

            [Option('p', "pause",
                HelpText = "Whether or not to wait for the user's input after the processing is done")]
            public bool PauseOnExit { get; set; }

            public bool IsGenerateOnly { get; set; }
        }
    }
}