using System;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArenaBookDbContext))]
    [Migration("20260513182205_PayPalWebhooksAndIdempotencyIndex")]
    partial class PayPalWebhooksAndIdempotencyIndex
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ArenaBook.Domain.Entities.City", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CountryId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.HasKey("Id");

                    b.HasIndex("CountryId");

                    b.ToTable("Cities", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            CountryId = 1,
                            Name = "Mostar"
                        },
                        new
                        {
                            Id = 2,
                            CountryId = 1,
                            Name = "Sarajevo"
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.CoinLedgerEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("AmountCoins")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("BalanceAfter")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("ReasonCode")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<int?>("RelatedScheduledSessionId")
                        .HasColumnType("int");

                    b.Property<int>("UserCoinWalletId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RelatedScheduledSessionId");

                    b.HasIndex("UserCoinWalletId");

                    b.ToTable("CoinLedgerEntries", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.Country", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.HasKey("Id");

                    b.ToTable("Countries", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Bosna i Hercegovina"
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.EquipmentType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.HasKey("Id");

                    b.ToTable("EquipmentTypes", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Vještačka trava"
                        },
                        new
                        {
                            Id = 2,
                            Name = "LED rasvjeta"
                        },
                        new
                        {
                            Id = 3,
                            Name = "Gol mreže i konstrukcija"
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ExternalPaymentRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("AmountMoney")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("CoinsPurchased")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("nvarchar(3)");

                    b.Property<string>("ExternalReference")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("IdempotencyKey")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<int>("PaymentProcessingStatusId")
                        .HasColumnType("int");

                    b.Property<string>("Provider")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("PurposeCode")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("PaymentProcessingStatusId");

                    b.HasIndex("UserId", "IdempotencyKey", "Provider")
                        .IsUnique()
                        .HasFilter("[IdempotencyKey] IS NOT NULL");

                    b.ToTable("ExternalPaymentRecords", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.Hall", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CapacityPeople")
                        .HasColumnType("int");

                    b.Property<int>("CityId")
                        .HasColumnType("int");

                    b.Property<string>("ContactPhone")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<decimal?>("Latitude")
                        .HasPrecision(9, 6)
                        .HasColumnType("decimal(9,6)");

                    b.Property<decimal?>("Longitude")
                        .HasPrecision(9, 6)
                        .HasColumnType("decimal(9,6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<decimal>("PricePerHourCoins")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("StreetAddress")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.HasKey("Id");

                    b.HasIndex("CityId");

                    b.ToTable("Halls", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.HallEquipment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EquipmentTypeId")
                        .HasColumnType("int");

                    b.Property<int>("HallId")
                        .HasColumnType("int");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("EquipmentTypeId");

                    b.HasIndex("HallId", "EquipmentTypeId")
                        .IsUnique();

                    b.ToTable("HallEquipments", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.HallPhoto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("HallId")
                        .HasColumnType("int");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<int>("SortOrder")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("HallId", "SortOrder");

                    b.ToTable("HallPhotos", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.HallReview", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Comment")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("HallId")
                        .HasColumnType("int");

                    b.Property<byte>("RatingStars")
                        .HasColumnType("tinyint");

                    b.Property<int?>("ScheduledSessionId")
                        .HasColumnType("int");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("ScheduledSessionId");

                    b.HasIndex("UserId");

                    b.HasIndex("HallId", "UserId");

                    b.ToTable("HallReviews", null, t =>
                        {
                            t.HasCheckConstraint("CK_HallReviews_RatingStars", "[RatingStars] >= 1 AND [RatingStars] <= 5");
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.PayPalWebhookEventReceipt", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("PayPalEventId")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime>("ReceivedUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("PayPalEventId")
                        .IsUnique();

                    b.ToTable("PayPalWebhookEventReceipts", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.PaymentProcessingStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("PaymentProcessingStatuses", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Code = "PENDING",
                            DisplayName = "Na čekanju"
                        },
                        new
                        {
                            Id = 2,
                            Code = "COMPLETED",
                            DisplayName = "Uspješno"
                        },
                        new
                        {
                            Id = 3,
                            Code = "CANCELLED",
                            DisplayName = "Otkazano"
                        },
                        new
                        {
                            Id = 4,
                            Code = "FAILED",
                            DisplayName = "Neuspješno"
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.PlatformSettingEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("SettingKey")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("SettingValue")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("nvarchar(4000)");

                    b.Property<DateTime>("UpdatedUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SettingKey")
                        .IsUnique();

                    b.ToTable("PlatformSettingEntries", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSession", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("HallId")
                        .HasColumnType("int");

                    b.Property<string>("InviteCode")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<int?>("MaxAgeYears")
                        .HasColumnType("int");

                    b.Property<int>("MaxParticipants")
                        .HasColumnType("int");

                    b.Property<string>("OrganizerUserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("SessionKindId")
                        .HasColumnType("int");

                    b.Property<int>("SessionLifecycleStatusId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("HallId");

                    b.HasIndex("InviteCode")
                        .IsUnique()
                        .HasFilter("[InviteCode] IS NOT NULL");

                    b.HasIndex("OrganizerUserId");

                    b.HasIndex("SessionKindId");

                    b.HasIndex("SessionLifecycleStatusId");

                    b.ToTable("ScheduledSessions", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSessionAuditEntry", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("ActorUserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("DetailsJson")
                        .HasMaxLength(4000)
                        .HasColumnType("nvarchar(4000)");

                    b.Property<int?>("FromLifecycleStatusId")
                        .HasColumnType("int");

                    b.Property<DateTime>("OccurredUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("SessionId")
                        .HasColumnType("int");

                    b.Property<int?>("ToLifecycleStatusId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ActorUserId");

                    b.HasIndex("OccurredUtc");

                    b.HasIndex("SessionId");

                    b.ToTable("ScheduledSessionAuditEntries", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSessionParticipant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("CoinsPaid")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<bool>("IsOrganizer")
                        .HasColumnType("bit");

                    b.Property<DateTime>("JoinedUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("ScheduledSessionId")
                        .HasColumnType("int");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("ScheduledSessionId", "UserId")
                        .IsUnique();

                    b.ToTable("ScheduledSessionParticipants", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.SessionKind", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("SessionKinds", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Code = "PUBLIC",
                            DisplayName = "Javni termin"
                        },
                        new
                        {
                            Id = 2,
                            Code = "INVITE",
                            DisplayName = "Privatni poziv"
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.SessionLifecycleStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("SessionLifecycleStatuses", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Code = "PENDING",
                            DisplayName = "Na čekanju"
                        },
                        new
                        {
                            Id = 2,
                            Code = "CONFIRMED",
                            DisplayName = "Potvrđeno"
                        },
                        new
                        {
                            Id = 3,
                            Code = "CANCELLED",
                            DisplayName = "Otkazano"
                        },
                        new
                        {
                            Id = 4,
                            Code = "COMPLETED",
                            DisplayName = "Završeno"
                        });
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.StripeWebhookEventReceipt", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("ReceivedUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("StripeEventId")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasKey("Id");

                    b.HasIndex("StripeEventId")
                        .IsUnique();

                    b.ToTable("StripeWebhookEventReceipts", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.UserCoinWallet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("BalanceCoins")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("UpdatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserCoinWallets", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.UserNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ReadAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("TypeCode")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "ReadAtUtc", "CreatedUtc");

                    b.ToTable("UserNotifications", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Infrastructure.Identity.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<int?>("CityId")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("DateOfBirth")
                        .HasColumnType("date");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("ProfileImageUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("CityId");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.City", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.Country", "Country")
                        .WithMany("Cities")
                        .HasForeignKey("CountryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Country");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.CoinLedgerEntry", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.ScheduledSession", "RelatedScheduledSession")
                        .WithMany("RelatedLedgerEntries")
                        .HasForeignKey("RelatedScheduledSessionId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("ArenaBook.Domain.Entities.UserCoinWallet", "UserCoinWallet")
                        .WithMany("LedgerEntries")
                        .HasForeignKey("UserCoinWalletId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RelatedScheduledSession");

                    b.Navigation("UserCoinWallet");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ExternalPaymentRecord", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.PaymentProcessingStatus", "PaymentProcessingStatus")
                        .WithMany("ExternalPayments")
                        .HasForeignKey("PaymentProcessingStatusId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("PaymentProcessingStatus");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.Hall", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.City", "City")
                        .WithMany("Halls")
                        .HasForeignKey("CityId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("City");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.HallEquipment", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.EquipmentType", "EquipmentType")
                        .WithMany("HallEquipments")
                        .HasForeignKey("EquipmentTypeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ArenaBook.Domain.Entities.Hall", "Hall")
                        .WithMany("EquipmentLinks")
                        .HasForeignKey("HallId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("EquipmentType");

                    b.Navigation("Hall");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.HallPhoto", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.Hall", "Hall")
                        .WithMany("Photos")
                        .HasForeignKey("HallId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Hall");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.HallReview", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.Hall", "Hall")
                        .WithMany("Reviews")
                        .HasForeignKey("HallId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ArenaBook.Domain.Entities.ScheduledSession", "ScheduledSession")
                        .WithMany("HallReviews")
                        .HasForeignKey("ScheduledSessionId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Hall");

                    b.Navigation("ScheduledSession");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSession", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.Hall", "Hall")
                        .WithMany("ScheduledSessions")
                        .HasForeignKey("HallId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("OrganizerUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ArenaBook.Domain.Entities.SessionKind", "SessionKind")
                        .WithMany("ScheduledSessions")
                        .HasForeignKey("SessionKindId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ArenaBook.Domain.Entities.SessionLifecycleStatus", "SessionLifecycleStatus")
                        .WithMany("ScheduledSessions")
                        .HasForeignKey("SessionLifecycleStatusId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Hall");

                    b.Navigation("SessionKind");

                    b.Navigation("SessionLifecycleStatus");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSessionAuditEntry", b =>
                {
                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("ActorUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSessionParticipant", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.ScheduledSession", "ScheduledSession")
                        .WithMany("Participants")
                        .HasForeignKey("ScheduledSessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ScheduledSession");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.UserCoinWallet", b =>
                {
                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.UserNotification", b =>
                {
                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ArenaBook.Infrastructure.Identity.ApplicationUser", b =>
                {
                    b.HasOne("ArenaBook.Domain.Entities.City", "City")
                        .WithMany()
                        .HasForeignKey("CityId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("City");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("ArenaBook.Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.City", b =>
                {
                    b.Navigation("Halls");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.Country", b =>
                {
                    b.Navigation("Cities");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.EquipmentType", b =>
                {
                    b.Navigation("HallEquipments");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.Hall", b =>
                {
                    b.Navigation("EquipmentLinks");

                    b.Navigation("Photos");

                    b.Navigation("Reviews");

                    b.Navigation("ScheduledSessions");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.PaymentProcessingStatus", b =>
                {
                    b.Navigation("ExternalPayments");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.ScheduledSession", b =>
                {
                    b.Navigation("HallReviews");

                    b.Navigation("Participants");

                    b.Navigation("RelatedLedgerEntries");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.SessionKind", b =>
                {
                    b.Navigation("ScheduledSessions");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.SessionLifecycleStatus", b =>
                {
                    b.Navigation("ScheduledSessions");
                });

            modelBuilder.Entity("ArenaBook.Domain.Entities.UserCoinWallet", b =>
                {
                    b.Navigation("LedgerEntries");
                });
#pragma warning restore 612, 618
        }
    }
}

