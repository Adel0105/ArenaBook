using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    public partial class Day7PaymentsNotificationsWorker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalPaymentRecords_UserId",
                table: "ExternalPaymentRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "CoinsPurchased",
                table: "ExternalPaymentRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "ExternalPaymentRecords",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StripeWebhookEventReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StripeEventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReceivedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEventReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentRecords_UserId_IdempotencyKey",
                table: "ExternalPaymentRecords",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEventReceipts_StripeEventId",
                table: "StripeWebhookEventReceipts",
                column: "StripeEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_ReadAtUtc_CreatedUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "ReadAtUtc", "CreatedUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeWebhookEventReceipts");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_ExternalPaymentRecords_UserId_IdempotencyKey",
                table: "ExternalPaymentRecords");

            migrationBuilder.DropColumn(
                name: "CoinsPurchased",
                table: "ExternalPaymentRecords");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ExternalPaymentRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentRecords_UserId",
                table: "ExternalPaymentRecords",
                column: "UserId");
        }
    }
}

