using OneBot.Base;
using OneBot.Models;

namespace OneBot.SpamBroker
{
    public static class Extensions
    {
        public static bool IsSpam(this StateSpam stateSpam) => stateSpam == StateSpam.Forbidden || stateSpam == StateSpam.ForbiddenFirst;
        public static async Task<StateSpam> CheckMessageSpamStatus<TUser>(this ISpam<TUser> filter, ReceptionClient<TUser> message, SendingClient messageFirstSpam) where TUser : BaseUser
        {
            var state = filter.GetSpamState(message);
            if (state == StateSpam.ForbiddenFirst)
                await message.Send(messageFirstSpam);
            return state;
        }
        public static async Task<bool> IsSpan<TUser>(this ISpam<TUser> filter, ReceptionClient<TUser> message, SendingClient messageFirstSpam) where TUser : BaseUser
            => (await filter.CheckMessageSpamStatus(message, messageFirstSpam)).IsSpam();
    }
}
