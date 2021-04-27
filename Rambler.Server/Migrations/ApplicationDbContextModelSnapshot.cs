﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Rambler.Contracts.Api;
using Rambler.Server.Database;
using Rambler.Server.Database.Models;
using System;

namespace Rambler.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<Guid>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<Guid>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId");

                    b.Property<Guid>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ApplicationRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<DateTime>("LastSeenDate");

                    b.Property<int>("Level");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<int>("MaxRooms");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<DateTime>("RegistrationDate");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.Bot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<string>("Description");

                    b.Property<string>("EndPoint");

                    b.Property<bool>("IsEnabled");

                    b.Property<DateTime>("LastModified");

                    b.Property<string>("Name");

                    b.Property<Guid>("OwnerId");

                    b.Property<string>("Token");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("OwnerId");

                    b.ToTable("Bots");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.BotChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Added");

                    b.Property<Guid>("BotId");

                    b.Property<Guid>("ChannelId");

                    b.HasKey("Id");

                    b.HasIndex("BotId");

                    b.ToTable("BotChannels");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.Channel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("AllowGuests");

                    b.Property<DateTime>("Created");

                    b.Property<string>("Description");

                    b.Property<bool>("IsSecret");

                    b.Property<DateTime>("LastActivity");

                    b.Property<DateTime>("LastModified");

                    b.Property<int>("MaxUsers");

                    b.Property<string>("Name");

                    b.Property<Guid>("OwnerId");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("OwnerId");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("ChannelId");

                    b.Property<DateTime>("Created");

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("Expires");

                    b.Property<int>("Level");

                    b.Property<string>("Reason");

                    b.Property<Guid?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("Expires");

                    b.HasIndex("UserId");

                    b.ToTable("ChannelBans");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelBanAddress", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BanId");

                    b.Property<int?>("ChannelBanId");

                    b.Property<string>("IPFilter");

                    b.HasKey("Id");

                    b.HasIndex("ChannelBanId");

                    b.HasIndex("IPFilter");

                    b.ToTable("ChannelBanAddresses");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelModerator", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("ChannelId");

                    b.Property<DateTime>("Created");

                    b.Property<int>("Level");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("UserId");

                    b.ToTable("ChannelModerators");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelPost", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedOn");

                    b.Property<string>("Message");

                    b.Property<string>("Nick");

                    b.Property<Guid>("Originator");

                    b.Property<Guid>("Subscription");

                    b.Property<string>("Type");

                    b.HasKey("Id");

                    b.HasIndex("CreatedOn");

                    b.HasIndex("Originator");

                    b.HasIndex("Subscription");

                    b.ToTable("ChannelPosts");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ServerBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BannedNick");

                    b.Property<Guid?>("BannedUserId");

                    b.Property<DateTime>("Created");

                    b.Property<Guid>("CreatedById");

                    b.Property<string>("CreatedByNick");

                    b.Property<DateTime>("Expires");

                    b.Property<string>("IPFilter");

                    b.Property<string>("Reason");

                    b.HasKey("Id");

                    b.HasIndex("BannedUserId");

                    b.HasIndex("Expires");

                    b.HasIndex("IPFilter");

                    b.ToTable("ServerBans");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.UserChannel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("ChannelId");

                    b.Property<DateTime>("JoinedOn");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("UserId");

                    b.ToTable("UserChannels");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.UserConnection", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("ConnectedOn");

                    b.Property<string>("IPAddress");

                    b.Property<bool>("IsGuest");

                    b.Property<string>("Nick");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ConnectedOn");

                    b.HasIndex("IPAddress");

                    b.ToTable("UserConnections");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.UserIgnore", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("IgnoreId");

                    b.Property<string>("IgnoreNick");

                    b.Property<DateTime>("IgnoredOn");

                    b.Property<bool>("IsGuestIgnore");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("IgnoreId");

                    b.HasIndex("IsGuestIgnore");

                    b.HasIndex("UserId");

                    b.ToTable("UserIgnores");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.Bot", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.BotChannel", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.Bot")
                        .WithMany("Channels")
                        .HasForeignKey("BotId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.Channel", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser", "Owner")
                        .WithMany("Channels")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelBan", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.Channel", "Channel")
                        .WithMany("Bans")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelBanAddress", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ChannelBan")
                        .WithMany("Addresses")
                        .HasForeignKey("ChannelBanId");
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.ChannelModerator", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.Channel", "Channel")
                        .WithMany("Moderators")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.UserChannel", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Rambler.Server.Database.Models.UserIgnore", b =>
                {
                    b.HasOne("Rambler.Server.Database.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
