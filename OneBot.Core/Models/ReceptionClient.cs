using OneBot.Base;
using OneBot.Interfaces;

namespace OneBot.Models
{
    public record class ReceptionClient<TUser> : CollectionBotParameters where TUser : BaseUser
    {
        private readonly Func<SendingClient, ReceptionClient<TUser>?, Task> _send;
        private IReadOnlyList<MediaSource>? _medias;
        private string? _message;
        private string? _command;

        public ReceptionType ReceptionType;
        public IClientBot<TUser> Client;
        public TUser User;
        public object? OriginalMessage;
        public string? Message
        {
            get => _message;
            set => _message = string.IsNullOrWhiteSpace(value) ? null : value;
        }
        public IReadOnlyList<MediaSource>? Medias
        {
            get => _medias;
            set => _medias = (value?.Any() ?? false) ? value : null;
        }
        public string? Command
        {
            get => _command;
            set => _command = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public ReceptionClient(IClientBot<TUser> client, TUser user, Func<SendingClient, ReceptionClient<TUser>?, Task> send, ReceptionType receptionType)
        {
            Client=client??throw new ArgumentNullException(nameof(client));
            User=user??throw new ArgumentNullException(nameof(user));
            _send=send??throw new ArgumentNullException(nameof(send));
            ReceptionType=receptionType;
        }

        public Task Send(SendingClient model, ReceptionClient<TUser>? reception = null) => _send(model, reception);
    }

    [Flags]
    public enum ReceptionType
    {
        None = 0,
        Message = 1,
        Keyboard = 2,
        Inline = 4,
        Media = 8,
        Command = 16
    }
}
