﻿using Microsoft.Extensions.Logging;
using OneBot.Base;
using OneBot.Models;
using System.Collections.Concurrent;

namespace OneBot.SpamBroker
{
    public class BlackList<TUser>(ILogger<BlackList<TUser>>? logger) : ISpam<TUser> where TUser : BaseUser
    {
        private readonly ConcurrentDictionary<int, DateTime> _blackList = new();
        public int Count => _blackList.Count;

        public StateSpam GetSpamState(UpdateContext<TUser> context)
        {
            var now = DateTime.Now;
            if (_blackList.TryGetValue(context.User.Id, out DateTime date))
            {
                if (date <= now)
                {
                    _blackList.TryRemove(context.User.Id, out _);
                    logger?.LogInformation("Пользователь [{user}] удалён из чёрного списка", context.User);
                    return StateSpam.Allowed;
                }
                logger?.LogWarning("Пользователь [{user}] не смотря на блокировку продолжает слать сообщения", context.User);
                return StateSpam.Forbidden;
            }
            return StateSpam.Allowed;
        }

        public void AddBlock(TUser user, TimeSpan endBlock)
        {
            _blackList.TryAdd(user.Id, DateTime.Now + endBlock);
            logger?.LogWarning("Пользователь [{user}] был добавлен в чёрный список до {dateEnd}", user, endBlock);
        }
    }
}
