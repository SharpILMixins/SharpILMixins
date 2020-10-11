using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using NLog;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Processor
{
    class Program
    {
        [Verb("process", true)]
        public class ProcessOptions
        {
            [Option('t', "target-dir", Required = true)]
            public DirectoryInfo TargetDir { get; set; } = null!;

            [Option('m', "mixins", Required = true)]
            public IEnumerable<FileInfo> MixinsToApply { get; set; } = null!;

            [Option("dump-targets")] public bool DumpTargets { get; set; }

            [Option("experimental-inline-methods")]
            public bool ExperimentalInlineMethods { get; set; }

            [Option("mixin-handler-name")] public string MixinHandlerName { get; set; } = "mixin";
        }

        public static Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(Program));


        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ProcessOptions>(args)
                .WithParsed(o =>
                {
                    try
                    {
                        ProcessMixins(o);
                    }
                    catch (Exception e)
                    {
                        LogException(e, o);
                    }
                });
            if (!Debugger.IsAttached)
            {
                Logger.Info("Press any key to continue..");
                Console.ReadKey();
            }
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

            if (e.InnerException != null && e.InnerException != e)
            {
                LogException(e.InnerException, processOptions, true);
            }
        }

        private static void ProcessMixins(ProcessOptions o)
        {
            foreach (var mixinAssemblyFile in o.MixinsToApply)
            {
                Logger.Info($"Starting to process {mixinAssemblyFile.Name}");
                try
                {
                    var workspace = new MixinWorkspace(mixinAssemblyFile, o.TargetDir,
                        new MixinWorkspaceSettings(o.DumpTargets, o.MixinHandlerName, o.ExperimentalInlineMethods));

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
    }
}