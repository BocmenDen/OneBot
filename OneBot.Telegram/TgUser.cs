using OneBot.Base;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types;

namespace OneBot.Tg
{
    public class TgUser<TUser> where TUser : BaseUser
    {
        [Key]
        public long ChatId { get; set; }
        public TUser User { get; set; }
        public int UserId { get; set; }

        public TgUser(long chatId, TUser user)
        {
            ChatId = chatId;
            User = user;
            UserId = user.Id;
        }

        public TgUser(long chatId, int userId)
        {
            ChatId=chatId;
            UserId=userId;
            User = null!;
        }

        public static implicit operator ChatId(TgUser<TUser> tgUser) => tgUser.ChatId;

        public override string ToString() => $"Chat: {ChatId} User -> {User}";
    }
}
