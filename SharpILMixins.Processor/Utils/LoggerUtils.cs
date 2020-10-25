using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace SharpILMixins.Processor.Utils
{
    public static class LoggerUtils
    {
        public static LogFactory LogFactory { get; set; } = new LogFactory(CreateLoggingConfiguration());

        internal static LoggingConfiguration CreateLoggingConfiguration()
        {
            var config = new LoggingConfiguration();
            var layout = new SimpleLayout("[${time}] [${level:uppercase=true}/${logger}]: ${message}");

            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = layout
            };
            ConfigureTarget(consoleTarget, config);

            //ConfigureFileTarget(layout, config);
            return config;
        }

        private static void ConfigureFileTarget(SimpleLayout layout, LoggingConfiguration config)
        {
            var fileTarget = new FileTarget("file")
            {
                FileName = Layout.FromString("output.log"),
                Layout = layout
            };
            ConfigureTarget(fileTarget, config);
        }

        private static void ConfigureTarget(Target target, LoggingConfiguration config)
        {
            if (target is ColoredConsoleTarget consoleTarget)
                AddLogLevelRule(consoleTarget, LogLevel.Warn, ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange);
            config.AddTarget(target);
            config.AddRule(Utilities.DebugMode ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal, target);
        }

        private static void AddLogLevelRule(ColoredConsoleTarget consoleTarget, LogLevel level,
            ConsoleOutputColor foregroundColor,
            ConsoleOutputColor backgroundColor)
        {
            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                $"level == {nameof(LogLevel)}.{level.Name}", foregroundColor, backgroundColor));
        }
    }
}