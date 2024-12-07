using Microsoft.EntityFrameworkCore;
using OneBot.Attributes;
using OneBot.Base;
using OneBot.Tg;

namespace OneBots.Demo
{
    [Service(Type = ServiceType.DbContextPool)]
    public class DataBase(DbContextOptions options) : BaseDB<BaseUser>(options), IDBTg<BaseUser>
    {
        public DbSet<TgUser<BaseUser>> TgUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigurateDBTg<BaseUser>();
        }
    }
}
