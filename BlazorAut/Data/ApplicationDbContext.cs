using BlazorAut.Pages;
using BlazorAut.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorAut.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<AuthCode> AuthCodes { get; set; }
        public DbSet<AuthCodeHistory> AuthCodeHistories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<DbServerInfo> DbServerInfo { get; set; }
        public DbSet<AzureAdOptions> AzureAdOptions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<PinCode> PinCodes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRole>().ToTable("UserRole");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<AuthCodeHistory>().ToTable("AuthCodeHistory");
            modelBuilder.Entity<PinCode>().ToTable("PinCodes");

            modelBuilder.Entity<UserToken>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.Tokens)
                .HasForeignKey(ut => ut.UserId);
            modelBuilder.Entity<DbServerInfo>()
                .HasNoKey();

            modelBuilder.Entity<UserRole>()
               .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<AuthCodeHistory>().HasKey(ach => ach.Id);
        }
    }

}
