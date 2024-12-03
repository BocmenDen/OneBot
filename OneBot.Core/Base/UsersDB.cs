using Microsoft.EntityFrameworkCore;
using OneBot.Attributes;

namespace OneBot.Base
{
    [ServiceDB]
    [ServiceGenericInfo(TypesGeneric.User)]
    public class UsersDB<TUser> : DbContext where TUser : BaseUser
    {
        public virtual DbSet<TUser> Users { get; protected set; } = null!;

        public UsersDB(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BaseUser>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<BaseUser>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            base.OnModelCreating(modelBuilder);
        }
    }
}
