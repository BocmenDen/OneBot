using Microsoft.EntityFrameworkCore;
using OneBot.Base;

namespace OneBot.Tg
{
    public interface IDBTg<TUser> where TUser : BaseUser
    {
        public DbSet<TgUser<TUser>> TgUsers { get; }
    }
}
