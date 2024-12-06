using Microsoft.Extensions.Logging;
using OneBot.Base;
using OneBot.Models;
using System.Collections.Concurrent;

namespace OneBot.SpamBroker
{
    public class SingleMessageQueue<TValue, TUser>(
            Func<ReceptionClient<TUser>, TValue> getValue,
            ILogger<SingleMessageQueue<TValue, TUser>>? logger
        ) : ISpam<TUser>
        where TUser : BaseUser
        where TValue : notnull
    {
        private readonly ConcurrentDictionary<TValue, bool> _eventHistory = new();

        public StateSpam GetSpamState(ReceptionClient<TUser> message)
        {
            var key = getValue(message);
            if (_eventHistory.TryGetValue(key, out bool isSend))
            {
                if (isSend)
                {
                    logger?.LogWarning(Consts.EventIdForbidden, Consts.LogForbidden, message.User);
                    return StateSpam.Forbidden;
                }
                else
                {
                    _eventHistory.TryUpdate(key, true, false);
                    logger?.LogInformation(Consts.EventIdForbiddenFirst, Consts.LogForbiddenFirst, message.User);
                    return StateSpam.ForbiddenFirst;
                }
            }
            return StateSpam.Allowed;
        }

        public void RegisterEvent(ReceptionClient<TUser> message)
        {
            _eventHistory.TryAdd(getValue(message), false);
        }

        public void UnregisterEvent(ReceptionClient<TUser> message)
        {
            _eventHistory.TryRemove(getValue(message), out bool _);
        }
    }
}
