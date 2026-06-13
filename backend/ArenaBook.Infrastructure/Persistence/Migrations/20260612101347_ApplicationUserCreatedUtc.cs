using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationUserCreatedUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE u
                SET CreatedUtc = COALESCE(
                    ledger.EarliestLedgerUtc,
                    participant.EarliestJoinedUtc,
                    session.EarliestSessionUtc,
                    SYSUTCDATETIME())
                FROM AspNetUsers u
                OUTER APPLY (
                    SELECT MIN(e.CreatedUtc) AS EarliestLedgerUtc
                    FROM CoinLedgerEntries e
                    INNER JOIN UserCoinWallets w ON e.UserCoinWalletId = w.Id
                    WHERE w.UserId = u.Id
                ) ledger
                OUTER APPLY (
                    SELECT MIN(p.JoinedUtc) AS EarliestJoinedUtc
                    FROM ScheduledSessionParticipants p
                    WHERE p.UserId = u.Id
                ) participant
                OUTER APPLY (
                    SELECT MIN(s.CreatedUtc) AS EarliestSessionUtc
                    FROM ScheduledSessions s
                    WHERE s.OrganizerUserId = u.Id
                ) session
                WHERE u.CreatedUtc = '0001-01-01'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "AspNetUsers");
        }
    }
}
