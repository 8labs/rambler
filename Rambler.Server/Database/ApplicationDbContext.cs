namespace Rambler.Server.Database
{
    using Contracts.Responses;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using System;
    using System.Threading.Tasks;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public DbSet<ChannelPost> ChannelPosts { get; set; }

        public DbSet<Channel> Channels { get; set; }

        public DbSet<ChannelBan> ChannelBans { get; set; }

        public DbSet<ChannelBanAddress> ChannelBanAddresses { get; set; }

        public DbSet<ChannelModerator> ChannelModerators { get; set; }

        public DbSet<ServerBan> ServerBans { get; set; }

        public DbSet<UserChannel> UserChannels { get; set; }

        public DbSet<UserIgnore> UserIgnores { get; set; }

        public DbSet<UserConnection> UserConnections { get; set; }

        public DbSet<Bot> Bots { get; set; }

        public DbSet<BotChannel> BotChannels { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Channel>()
                .HasIndex(c => c.Name)
                .IsUnique();

            builder.Entity<ChannelPost>()
                .HasIndex(c => c.CreatedOn);

            builder.Entity<ChannelPost>()
                .HasIndex(c => c.Subscription);

            builder.Entity<ChannelPost>()
                .HasIndex(c => c.Originator);

            builder.Entity<ChannelBan>()
                .HasIndex(c => c.UserId);

            builder.Entity<ChannelBan>()
                .HasIndex(c => c.Expires);

            builder.Entity<ChannelBanAddress>()
                .HasIndex(c => c.IPFilter);

            builder.Entity<ServerBan>()
                .HasIndex(c => c.Expires);

            builder.Entity<ServerBan>()
                .HasIndex(c => c.IPFilter);

            builder.Entity<ServerBan>()
                .HasIndex(c => c.BannedUserId);

            builder.Entity<UserConnection>()
                .HasIndex(c => c.ConnectedOn);

            builder.Entity<UserConnection>()
                .HasIndex(c => c.IPAddress);

            builder.Entity<UserIgnore>()
                .HasIndex(c => c.IgnoreId);

            builder.Entity<UserIgnore>()
                .HasIndex(c => c.IsGuestIgnore);

            builder.Entity<Bot>()
                .HasIndex(b => b.Name)
                .IsUnique();

            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public async Task<long> SavePost(
            Guid subscription,
            Guid originator,
            DateTime timestamp,
            string message,
            string nick,
            string type)
        {
            var sresp = new ChannelPost()
            {
                CreatedOn = timestamp,
                Originator = originator,
                Nick = nick,
                Subscription = subscription,
                Message = message,
                Type = type,
            };

            ChannelPosts.Add(sresp);
            await SaveChangesAsync();

            return sresp.Id;
        }
    }
}
