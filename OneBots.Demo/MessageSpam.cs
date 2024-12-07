using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneBot.Attributes;
using OneBot.Base;
using OneBot.Models;
using OneBot.SpamBroker;

namespace OneBots.Demo
{
    [Service(Type = ServiceType.Singltone)]
    public class MessageSpam<TUser> where TUser : BaseUser
    {
        private readonly SpamBroker<int, TUser> _spamFilter;
        private readonly SingleMessageQueue<int, TUser> _singleMessageFilter;
        private readonly BlackList<TUser> _blackList;
        private readonly ILogger _logger;
        private Func<ReceptionClient<TUser>, Task>? _action;
        private readonly TimeSpan _banTime;

        public MessageSpam(
            IConfiguration configuration,
            ILogger<MessageSpam<TUser>> logger,
            ILogger<SpamBroker<int, TUser>> loggerSpam,
            ILogger<SingleMessageQueue<int, TUser>> loggerSingleSpam,
            ILogger<BlackList<TUser>> blackListLogger
            )
        {
            _singleMessageFilter = new(
                (u) => u.User.Id,
                loggerSingleSpam
            );
            _spamFilter = new(
                (u) => u.User.Id,
                configuration.GetValue<int?>("spam_countMessage") ?? 5,
                configuration.GetValue<TimeSpan?>("spam_timeWindow") ?? TimeSpan.FromSeconds(3),
                loggerSpam
            );
            _blackList = new(blackListLogger);
            _logger=logger;
            _banTime=configuration.GetValue<TimeSpan?>("spam_timeBan") ?? TimeSpan.FromMinutes(5);
        }

        public void Init(Func<ReceptionClient<TUser>, Task> action)
        {
            _action = action;
        }

        public async void HandleCommand(ReceptionClient<TUser> updateData)
        {
            if (_blackList.GetSpamState(updateData).IsSpam() ||
                (await _singleMessageFilter.CheckMessageSpamStatus(updateData, "Пожалуйста, подождите немного! ✨ Ваше сообщение обрабатывается… ⚙️")).IsSpam()
                ) return;
            var state = await _spamFilter.CheckMessageSpamStatus(updateData, $"Вы превысели {_spamFilter.MaxEvent} сообщений за {ConvertTimeSpan(_spamFilter.TimeWindow)}, выдан бан на {ConvertTimeSpan(_banTime)}");
            if (state == StateSpam.ForbiddenFirst)
                _blackList.AddBlock(updateData.User, _banTime);
            if (state.IsSpam()) return;
            _singleMessageFilter.RegisterEvent(updateData);
            await HandleMessage(updateData);
            _singleMessageFilter.UnregisterEvent(updateData);
        }

        private async Task HandleMessage(ReceptionClient<TUser> updateData)
        {
            if (_action == null) return;
            try
            {
                await _action(updateData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "При обработке сообщения [{message}] у пользователя [{user}] произошла ошибка", updateData, updateData.User);
                _ = updateData.Send($"Произошла ошибка при обработке сообщения {ex.Message}");
            }
        }

        private static string ConvertTimeSpan(TimeSpan timeSpan)
        {
            string readableTimeSpan = "";
            if (timeSpan.Days > 0)
                readableTimeSpan += timeSpan.Days + " дн. ";
            if (timeSpan.Hours > 0)
                readableTimeSpan += timeSpan.Hours + " ч. ";
            if (timeSpan.Minutes > 0)
                readableTimeSpan += timeSpan.Minutes + " мин. ";
            if (timeSpan.Seconds > 0)
                readableTimeSpan += timeSpan.Seconds + " сек.";
            return readableTimeSpan;
        }
    }
}
