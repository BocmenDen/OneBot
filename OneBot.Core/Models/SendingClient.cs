using System.Text;

namespace OneBot.Models
{
    public record class SendingClient : CollectionBotParameters
    {
        private IReadOnlyList<MediaSource>? _medias;

        public string? Message;
        public ButtonsSend? Keyboard;
        public ButtonsSend? Inline;
        public IReadOnlyList<MediaSource>? Medias
        {
            get => _medias;
            set => _medias = (value?.Any() ?? false) ? value : null;
        }

        public static implicit operator SendingClient(string text) => new() { Message = text };
        public static implicit operator SendingClient(StringBuilder builder) => builder.ToString();

        public static SendingClient operator +(SendingClient sending, string text) => sending.Message += text;
    }
}
