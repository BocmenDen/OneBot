namespace OneBot.Interfaces
{
    public interface ILogger
    {
        public void Log(string message, LogTypes logTypes = LogTypes.Info, int? senderId = null);

        public enum LogTypes
        {
            Info,
            Warning,
            Error,
        }
    }
}
