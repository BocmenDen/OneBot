using OneBot.Interfaces;
using OneBot.Utils;

namespace OneBot.Extensions
{
    public static class LoggerExtensions
    {
        public static void Info(this ILogger? logger, string message, int? senderId = null) => logger?.Log(message, ILogger.LogTypes.Info, senderId);
        public static void Warning(this ILogger? logger, string message, int? senderId = null) => logger?.Log(message, ILogger.LogTypes.Warning, senderId);
        public static void Error(this ILogger? logger, string message, int? senderId = null) => logger?.Log(message, ILogger.LogTypes.Error, senderId);

        public static ILogger? CacheSender<T>(this ILogger? logger, params object[] secret) => logger.CacheSender(SharedUtils.CalculeteID<T>(secret));
        public static ILogger? CacheSender(this ILogger? logger, int sender) => logger == null ? null : new LoggerCache(logger, sender);

        private class LoggerCache(ILogger logger, int senderId) : ILogger
        {
            private readonly ILogger _logger = logger??throw new ArgumentNullException(nameof(logger));
            private readonly int _senderId = senderId;

            public void Log(string message, ILogger.LogTypes logTypes = ILogger.LogTypes.Info, int? senderId = null) => _logger.Log(message, logTypes, _senderId);
        }
    }
}
