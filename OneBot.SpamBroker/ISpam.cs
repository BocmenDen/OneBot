using OneBot.Base;
using OneBot.Models;

namespace OneBot.SpamBroker
{
    public interface ISpam<TUser> where TUser : BaseUser
    {
        public StateSpam GetSpamState(UpdateContext<TUser> context);
    }
}
