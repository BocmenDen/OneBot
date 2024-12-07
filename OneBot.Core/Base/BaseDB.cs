using Microsoft.EntityFrameworkCore;

namespace OneBot.Base
{
    public class BaseDB<TUser> : DbContext where TUser : BaseUser
    {
        public virtual DbSet<TUser> Users { get; protected set; } = null!;

        public BaseDB(DbContextOptions options) : base(options)
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
