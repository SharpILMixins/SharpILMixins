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
            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = new SimpleLayout("[${time}] [${level:uppercase=true}/${logger}]: ${message}")
            };
            AddLogLevelRule(consoleTarget, LogLevel.Warn, ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange);
            config.AddTarget(consoleTarget);
            config.AddRule(Utilities.DebugMode ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal, consoleTarget);
            return config;
        }

        private static void AddLogLevelRule(ColoredConsoleTarget consoleTarget, LogLevel level, ConsoleOutputColor foregroundColor,
            ConsoleOutputColor backgroundColor)
        {
            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                $"level == {nameof(LogLevel)}.{level.Name}", foregroundColor, backgroundColor));
        }
    }
}