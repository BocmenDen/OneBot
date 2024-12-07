using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneBot.Attributes;
using OneBot.Base;
using OneBot.Interfaces;
using OneBot.Models;
using OneBot.Utils;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OneBot.Tg
{
    [Service]
    public partial class TgClient<TUser, TDB> : IClientBot<TUser> where TUser : BaseUser where TDB : BaseDB<TUser>, IDBTg<TUser>
    {
        private static readonly Regex _parseCommand = GetParseCommandRegex();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TgClient<TUser, TDB>>? _logger;
        public readonly TelegramBotClient BotClient;
        private readonly ReceiverOptions? _receiverOptions;
        private EventId _eventId;

        public int Id { get; private set; }
        public event Action<ReceptionClient<TUser>>? Update;

        public TgClient(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<TgClient<TUser, TDB>>? logger = null, ReceiverOptions? receiverOptions = null)
        {
            _serviceProvider=serviceProvider??throw new ArgumentNullException(nameof(serviceProvider));
            string token = configuration[TgClient.KeySettingTOKEN] ?? throw new Exception("Отсутствует токен для создания клиента Telegram");
            Id = token.GetHashCode();
            BotClient = new TelegramBotClient(token);
            _receiverOptions = receiverOptions;
            _logger = logger;
            _eventId = new EventId(Id);
        }

        public async Task Run(CancellationToken token = default)
        {
            Task task = BotClient.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, _receiverOptions, cancellationToken: token);
            string botName = (await BotClient.GetMyName(cancellationToken: token)).Name;
            _logger?.LogInformation("Бот {botName} запущен", botName);
            _eventId = new EventId(Id, botName);
            await task;
        }

        public void RegisterUpdateHadler(Action<ReceptionClient<TUser>> action) => Update += action;
        public void UnregisterUpdateHadler(Action<ReceptionClient<TUser>> action) => Update -= action;

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (Update == null) return Task.CompletedTask;
            if (!TryGetInfoUser(update, out long? chatId, out TgUser<TUser>? user)) return Task.CompletedTask;
            var media = GetMedias(update, out var flagType);
            var command = TgClient<TUser, TDB>.ParseCommand(update?.Message?.Text);
            Update?.Invoke(new ReceptionClient<TUser>(this, user!.User,
                (d, r) => Send(user, d, r),
                TgClient<TUser, TDB>.GetReceptionType(update!) | flagType | (command == null ? ReceptionType.None : ReceptionType.Command)
                )
            {
                OriginalMessage = update,
                Message = update?.Message?.Text ?? update?.Message?.Caption,
                Medias = media,
                Command = command
            });
            return Task.CompletedTask;
        }

        public async Task Send(TgUser<TUser> user, SendingClient sendingClient, ReceptionClient<TUser>? reception = null)
        {
            if (sendingClient.Message == null)
            {
                _logger?.LogWarning(_eventId, "Поддерживается пока только отправка текстовых сообщений");
                return;
            }

            async Task sendMessageSaveId(Func<Task<Message>> action)
            {
                var message = await action();
                sendingClient[TgClient.MessegesToEdit] = message.Id;
                if (reception != null) reception[TgClient.MessegesToEdit] = message.Id;
            }

            if (sendingClient.Medias != null)
            {
                foreach (var doc in sendingClient.Medias)
                {
                    using var file = await doc.GetFile();
                    var message = await BotClient.SendDocument(user, file, caption: sendingClient.Message!, replyMarkup: TgClient<TUser, TDB>.GetReplyMarkup(sendingClient), parseMode: sendingClient.GetParseMode());
                    doc[TgClient.KeyMediaSourceFileId] = message.Document!.FileId;
                }
                return;
            }

            if (sendingClient.ContainsKey(TgClient.MessegesToEdit) && sendingClient.Inline == null && sendingClient.Keyboard == null)
                await EditMessage(user, (int)sendingClient[TgClient.MessegesToEdit], sendingClient, reception, sendMessageSaveId);
            else
                await sendMessageSaveId(() => BotClient.SendMessage(user, sendingClient.Message!, replyMarkup: TgClient<TUser, TDB>.GetReplyMarkup(sendingClient), parseMode: sendingClient.GetParseMode()));
        }

        private static IReplyMarkup? GetReplyMarkup(SendingClient sendingClient)
            => (IReplyMarkup?)sendingClient.Inline.CreateTgInline() ?? sendingClient.Keyboard.CreateTgReply();

#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
        private Task EditMessage(TgUser<TUser> user, int oldMessage, SendingClient sendingClient, ReceptionClient<TUser>? reception, Func<Func<Task<Message>>, Task> sendMessageSaveId)
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр
        {
            return sendMessageSaveId(() => BotClient.EditMessageText(user, oldMessage, sendingClient.Message!, replyMarkup: sendingClient.Inline.CreateTgInline(), parseMode: sendingClient.GetParseMode()));
        }

        private bool TryGetInfoUser(Update update, out long? chatId, out TgUser<TUser>? user)
        {
            user = null;
            chatId = GetChatId(update);
            if (chatId == null) return false;
            user = LoadUser((long)chatId);
            return true;
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger?.LogError(_eventId, exception, "Внутренняя ошибка обработки сообщений");
            return Task.CompletedTask;
        }

        private TgUser<TUser> LoadUser(long chatId)
        {
            using var db = _serviceProvider.GetRequiredService<TDB>();
            var telegramUser = db.TgUsers.Find(chatId);
            if (telegramUser != null)
            {
                telegramUser.User ??= db.Users.Find(telegramUser.UserId)!;
                return telegramUser!;
            }
            var user = BaseUserUtil.CreateEmptyUser<TUser>();
            telegramUser = new TgUser<TUser>(chatId, user);
            db.TgUsers.Add(telegramUser);
            db.SaveChanges();
            _logger?.LogInformation(_eventId, "Добавлен новый пользователь [{userTg}]", telegramUser);
            return telegramUser;
        }

        public static long? GetChatId(Update update)
        {
            if (update.Message != null) return update.Message.Chat.Id;
            if (update.EditedMessage != null) return update.EditedMessage.Chat.Id;
            if (update.ChannelPost != null) return update.ChannelPost.Chat.Id;
            if (update.EditedChannelPost != null) return update.EditedChannelPost.Chat.Id;
            if (update.CallbackQuery != null && update.CallbackQuery.Message != null) return update.CallbackQuery.Message.Chat.Id;
            if (update.InlineQuery != null && update.InlineQuery.From != null) return update.InlineQuery.From.Id; // Для inline запросов это ID пользователя, не чата!
            if (update.ChosenInlineResult != null && update.ChosenInlineResult.From != null) return update.ChosenInlineResult.From.Id; // Для chosen inline results это ID пользователя, не чата!
            if (update.ShippingQuery != null && update.ShippingQuery.From != null) return update.ShippingQuery.From.Id; // ID пользователя
            if (update.PreCheckoutQuery != null && update.PreCheckoutQuery.From != null) return update.PreCheckoutQuery.From.Id;
            return null; // ChatId не найден
        }

        public ButtonSearch? GetIndexButton(ReceptionClient<TUser> client, ButtonsSend buttonsSend)
        {
            if (client.OriginalMessage is not Update update) return null;
            for (int i = 0; i < buttonsSend.Buttons.Count; i++)
            {
                for (int j = 0; j < buttonsSend.Buttons[i].Count; j++)
                {
                    if (buttonsSend.Buttons[i][j].Text == update.Message?.Text || buttonsSend.Buttons[i][j].Text == update.InlineQuery?.Query)
                    {
                        return new ButtonSearch(i, j, buttonsSend.Buttons[i][j]);
                    }
                }
            }
            return null;
        }

        private static ReceptionType GetReceptionType(Update update)
        {
            ReceptionType receptionType = update.Type switch
            {
                Telegram.Bot.Types.Enums.UpdateType.Message => ReceptionType.Message,
                Telegram.Bot.Types.Enums.UpdateType.InlineQuery => ReceptionType.Inline,
                Telegram.Bot.Types.Enums.UpdateType.CallbackQuery => ReceptionType.Keyboard,
                _ => ReceptionType.None
            };
            return receptionType;
        }

        private List<MediaSource>? GetMedias(Update update, out ReceptionType receptionType)
        {
            List<MediaSource> mediaSources = [];
            receptionType = ReceptionType.None;
            if (update.Message?.Document != null)
            {
                receptionType |= ReceptionType.Media;
                mediaSources.Add(new MediaSource(async () =>
                {
                    string path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + update.Message.Document.FileName!);
                    var streamWriter = System.IO.File.Open(path, FileMode.OpenOrCreate);
                    var file = await BotClient.GetFile(update.Message?.Document.FileId!);
                    await BotClient.DownloadFile(file.FilePath!, streamWriter);
                    streamWriter.Position = 0;
                    return streamWriter;
                }, new() { { TgClient.KeyMediaSourceFileId, update.Message.Document.FileId } })
                {
                    Name = update.Message.Document.FileName,
                    Type = Path.GetExtension(update.Message.Document.FileName),
                    MimeType = update.Message.Document.MimeType,
                    Id = update.Message.Document.FileId
                });
            } // TODO остальные медиаданные
            if (mediaSources.Count!=0) return mediaSources;
            return null;
        }

        private static string? ParseCommand(string? text)
        {
            if (text == null) return null;
            var match = _parseCommand.Match(text);
            if (match.Success)
                return match.Groups[1].Value;
            return null;
        }

        [GeneratedRegex(@"^/(\w+)(@\w+)?$", RegexOptions.Compiled)]
        private static partial Regex GetParseCommandRegex();
    }

    public static class TgClient
    {
        public const string KeySettingTOKEN = "tg_token";
        public const string MessegesToEdit = "tg_messagesToEdit";
        public const string KeyParseMode = "tg_parseMode";

        public const string KeyMediaSourceFileId = "tg_fileId";
    }
}
