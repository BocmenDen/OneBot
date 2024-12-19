using OneBot.Base;
using OneBot.Interfaces;

namespace OneBot.Models
{
    public record class UpdateContext<TUser> where TUser : BaseUser
    {
        public delegate Task SendFunction(SendModel send, TUser user, UpdateContext<TUser>? context);

        private readonly SendFunction _send;
        public readonly IClientBot<TUser> Client;
        public readonly TUser User;
        public readonly UpdateModel Update;

        public UpdateContext(IClientBot<TUser> client, TUser user, SendFunction send, UpdateModel update)
        {
            Client=client??throw new ArgumentNullException(nameof(client));
            User=user??throw new ArgumentNullException(nameof(user));
            _send=send??throw new ArgumentNullException(nameof(send));
            Update=update;
        }

        public Task Send(SendModel send, UpdateContext<TUser>? context = null) => _send(send, User, context);
    }
}
