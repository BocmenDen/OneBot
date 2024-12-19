using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    public partial class TgClient<TUser, TDB> : IClientBot<TUser>, IDisposable where TUser : BaseUser where TDB : BaseDB<TUser>, IDBTg<TUser>
    {
        private static readonly Regex _parseCommand = GetParseCommandRegex();
        private readonly ILogger<TgClient<TUser, TDB>>? _logger;
        public readonly TelegramBotClient BotClient;
        private readonly ReceiverOptions? _receiverOptions;
        private EventId _eventId;
        private readonly TDB _database;

        public int Id { get; private set; }
        public event Action<UpdateContext<TUser>>? Update;

        public TgClient(IConfiguration configuration, IDbContextFactory<TDB> factoryDB, ILogger<TgClient<TUser, TDB>>? logger = null, ReceiverOptions? receiverOptions = null)
        {
            string token = configuration[TgClient.KeySettingTOKEN] ?? throw new Exception("Отсутствует токен для создания клиента Telegram");
            Id = token.GetHashCode();
            BotClient = new TelegramBotClient(token);
            _receiverOptions = receiverOptions;
            _logger = logger;
            _eventId = new EventId(Id);
            _database=factoryDB.CreateDbContext();
        }

        public async Task Run(CancellationToken token = default)
        {
            Task task = BotClient.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, _receiverOptions, cancellationToken: token);
            string botName = (await BotClient.GetMyName(cancellationToken: token)).Name;
            _eventId = new EventId(Id, botName);
            _logger?.LogInformation(_eventId, "Бот {botName} запущен", botName);
            await task;
        }

        public void RegisterUpdateHadler(Action<UpdateContext<TUser>> action) => Update += action;
        public void UnregisterUpdateHadler(Action<UpdateContext<TUser>> action) => Update -= action;

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (Update == null) return Task.CompletedTask;
            if (!TryGetInfoUser(update, out long? chatId, out TgUser<TUser>? user)) return Task.CompletedTask;
            var media = GetMedias(update, out var flagType);
            var command = TgClient<TUser, TDB>.ParseCommand(update?.Message?.Text ?? update?.Message?.Caption);
            Update?.Invoke(new UpdateContext<TUser>(this, user!.User,
                (d, _, r) => Send(d, user, r),
                new UpdateModel()
                {
                    UpdateType = TgClient<TUser, TDB>.GetReceptionType(update!) | flagType | (command == null ? UpdateType.None : UpdateType.Command),
                    OriginalMessage = update,
                    Message = update?.Message?.Text ?? update?.Message?.Caption,
                    Medias = media,
                    Command = command
                }
                ));
            return Task.CompletedTask;
        }

        public async Task Send(SendModel send, TgUser<TUser> user, UpdateContext<TUser>? context = null)
        {
            if (send.Message == null)
            {
                _logger?.LogWarning(_eventId, "Поддерживается пока только отправка текстовых сообщений");
                return;
            }

            async Task sendMessageSaveId(Func<Task<Message>> action)
            {
                var message = await action();
                send[TgClient.MessegesToEdit] = message.Id;
                if (context != null) context.Update[TgClient.MessegesToEdit] = message.Id;
            }

            if (send.Medias != null)
            {
                foreach (var doc in send.Medias)
                {
                    using var file = await doc.GetFile();
                    Message? message;
                    if (doc.Name?.Contains(".mp4") ?? false)
                    {
                        message = await BotClient.SendVideo(user, file, caption: send.Message!, replyMarkup: TgClient<TUser, TDB>.GetReplyMarkup(send), parseMode: send.GetParseMode());
                        doc[TgClient.KeyMediaSourceFileId] = message.Video!.FileId;
                    }
                    else
                    {
                        message = await BotClient.SendDocument(user, file, caption: send.Message!, replyMarkup: TgClient<TUser, TDB>.GetReplyMarkup(send), parseMode: send.GetParseMode());
                        doc[TgClient.KeyMediaSourceFileId] = message.Document!.FileId;
                    }
                }
                return;
            }

            if (send.ContainsKey(TgClient.MessegesToEdit) && send.Inline == null && send.Keyboard == null)
                await EditMessage(user, (int)send[TgClient.MessegesToEdit], send, context, sendMessageSaveId);
            else
                await sendMessageSaveId(() => BotClient.SendMessage(user, send.Message!, replyMarkup: TgClient<TUser, TDB>.GetReplyMarkup(send), parseMode: send.GetParseMode()));
        }

        private static IReplyMarkup? GetReplyMarkup(SendModel sendingClient)
            => (IReplyMarkup?)sendingClient.Inline.CreateTgInline() ?? sendingClient.Keyboard.CreateTgReply();

#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
        private Task EditMessage(TgUser<TUser> user, int oldMessage, SendModel sendingClient, UpdateContext<TUser>? context, Func<Func<Task<Message>>, Task> sendMessageSaveId)
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
            _logger?.LogError(_eventId, exception, "Внутренняя ошибка работы клиента Tg");
            return Task.CompletedTask;
        }

        private TgUser<TUser> LoadUser(long chatId)
        {
            var telegramUser = _database.TgUsers.Find(chatId);
            if (telegramUser != null)
            {
                telegramUser.User ??= _database.Users.Find(telegramUser.UserId)!;
                return telegramUser!;
            }
            var user = BaseUserUtil.CreateEmptyUser<TUser>();
            telegramUser = new TgUser<TUser>(chatId, user);
            _database.TgUsers.Add(telegramUser);
            _database.SaveChanges();
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

        public ButtonSearch? GetIndexButton(UpdateContext<TUser> context, ButtonsSend buttonsSend)
        {
            if (context.Update.OriginalMessage is not Update update) return null;
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

        private static UpdateType GetReceptionType(Update update)
        {
            UpdateType receptionType = update.Type switch
            {
                Telegram.Bot.Types.Enums.UpdateType.Message => UpdateType.Message,
                Telegram.Bot.Types.Enums.UpdateType.InlineQuery => UpdateType.Inline,
                Telegram.Bot.Types.Enums.UpdateType.CallbackQuery => UpdateType.Keyboard,
                _ => UpdateType.None
            };
            return receptionType;
        }

        private List<MediaSource>? GetMedias(Update update, out UpdateType receptionType)
        {
            List<MediaSource> mediaSources = [];
            receptionType = UpdateType.None;
            if (update.Message?.Document != null)
                AddMedia(mediaSources, update.Message.Document, update.Message.Document.FileName, update.Message.Document.MimeType);
            else if (update.Message?.Animation != null)
                AddMedia(mediaSources, update.Message.Animation, update.Message.Animation.FileName, update.Message.Animation.MimeType);
            else if(update.Message?.Video != null)
                AddMedia(mediaSources, update.Message.Video, update.Message.Video.FileName, update.Message.Video.MimeType);
            if (mediaSources.Count!=0)
            {
                receptionType = UpdateType.Media;
                return mediaSources;
            }
            return null;
        }

        private void AddMedia(List<MediaSource> medias,  FileBase? fileBase, string? fileName, string? mimeType)
        {
            if (fileBase == null) return;
            medias.Add(new MediaSource(async () =>
            {
                string path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + fileName);
                var streamWriter = System.IO.File.Open(path, FileMode.OpenOrCreate);
                var file = await BotClient.GetFile(fileBase.FileId!);
                await BotClient.DownloadFile(file.FilePath!, streamWriter);
                streamWriter.Position = 0;
                return streamWriter;
            }, new() { { TgClient.KeyMediaSourceFileId, fileBase.FileId } })
            {
                Name = fileName,
                Type = Path.GetExtension(fileName),
                MimeType = mimeType,
                Id = fileBase.FileId
            });
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

        public void Dispose()
        {
            _database.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public static class TgClient
    {
        public const string KeySettingTOKEN = "tg_token";
        public const string MessegesToEdit = "tg_messagesToEdit";
        public const string KeyParseMode = "tg_parseMode";

        public const string KeyMediaSourceFileId = "tg_fileId";
    }
}
