using Microsoft.EntityFrameworkCore;
using OneBot.Attributes;
using OneBot.Extensions;
using OneBot.Utils;

namespace OneBot.Base
{
    [ServiceDB]
    [ServiceGenericInfo(TypesGeneric.User)]
    public class UsersDB<TUser> : DbContext where TUser : BaseUser
    {
        private static readonly Func<UsersDB<TUser>, int, TUser?> GetUserById = EF.CompileQuery((UsersDB<TUser> context, int id) => context.Users.AsNoTracking().FirstOrDefault(x => x.Id == id));

        public virtual DbSet<TUser> Users { get; protected set; } = null!;

        public UsersDB(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        public TUser? GetUser(int userId) => GetUserById(this, userId);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BaseUser>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<BaseUser>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            base.OnModelCreating(modelBuilder);
        }

        public TUser CreateUser(TUser? userBase = null) => this.CreateElementAndReload(userBase ?? BaseUserUtil.CreateEmptyUser<TUser>(), Users);
    }
}
