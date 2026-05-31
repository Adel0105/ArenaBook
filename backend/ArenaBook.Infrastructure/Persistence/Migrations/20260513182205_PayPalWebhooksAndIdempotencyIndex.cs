using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    public partial class PayPalWebhooksAndIdempotencyIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalPaymentRecords_UserId_IdempotencyKey",
                table: "ExternalPaymentRecords");

            migrationBuilder.CreateTable(
                name: "PayPalWebhookEventReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayPalEventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReceivedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayPalWebhookEventReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentRecords_UserId_IdempotencyKey_Provider",
                table: "ExternalPaymentRecords",
                columns: new[] { "UserId", "IdempotencyKey", "Provider" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalWebhookEventReceipts_PayPalEventId",
                table: "PayPalWebhookEventReceipts",
                column: "PayPalEventId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayPalWebhookEventReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ExternalPaymentRecords_UserId_IdempotencyKey_Provider",
                table: "ExternalPaymentRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentRecords_UserId_IdempotencyKey",
                table: "ExternalPaymentRecords",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }
    }
}

