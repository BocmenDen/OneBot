using Microsoft.Extensions.Logging;
using OneBot.Base;
using OneBot.Models;
using System.Collections.Concurrent;

namespace OneBot.SpamBroker
{
    public class SpamBroker<TValue, TUser>(
            Func<ReceptionClient<TUser>, TValue> getValue,
            int maxEvent, TimeSpan timeWindow,
            ILogger<SpamBroker<TValue, TUser>>? logger
        ) : ISpam<TUser>
        where TUser : BaseUser
        where TValue : notnull
    {
        public int MaxEvent => maxEvent;
        public TimeSpan TimeWindow => timeWindow;

        private readonly ConcurrentDictionary<TValue, ConcurrentQueue<DateTime>> _eventHistory = new();

        private void CheckInit()
        {
            if (getValue == null) throw new ArgumentNullException(nameof(CheckInit));
        }

        public StateSpam GetSpamState(ReceptionClient<TUser> message)
        {
            CheckInit();
            var identifier = getValue!(message);
            DateTime now = DateTime.UtcNow;

            var events = _eventHistory.GetOrAdd(identifier, _ => new ConcurrentQueue<DateTime>());
            while (events.TryPeek(out DateTime timestamp) && (now - timestamp) > timeWindow)
                events.TryDequeue(out _);
            events.Enqueue(now);
            if (events.Count > maxEvent)
            {
                logger?.LogWarning(Consts.EventIdForbidden, Consts.LogForbidden, message.User);
                return StateSpam.Forbidden;
            }
            else if (events.Count >= maxEvent)
            {
                logger?.LogWarning(Consts.EventIdForbiddenFirst, Consts.LogForbiddenFirst, message.User);
                return StateSpam.ForbiddenFirst;
            }
            return StateSpam.Allowed;
        }

        public int CleanupEmptyHistory()
        {
            CheckInit();
            int countUsers = 0;
            foreach (var key in _eventHistory.Keys.ToList())
            {
                if (_eventHistory.TryGetValue(key, out var events))
                {
                    if (events.IsEmpty)
                    {
                        _eventHistory.TryRemove(key, out _);
                        countUsers++;
                        logger?.LogInformation($"История пользователя с ключом {key} была удалена для экономии памяти");
                    }
                }
            }
            return countUsers;
        }
    }
}
