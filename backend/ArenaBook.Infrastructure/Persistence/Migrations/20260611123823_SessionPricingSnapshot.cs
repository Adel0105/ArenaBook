using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SessionPricingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerParticipantCoins",
                table: "ScheduledSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceTotalCoins",
                table: "ScheduledSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE ss
                SET
                    PriceTotalCoins = CAST(ROUND(
                        h.PricePerHourCoins *
                        CASE
                            WHEN DATEDIFF(minute, ss.StartUtc, ss.EndUtc) <= 0 THEN 0
                            WHEN CAST(DATEDIFF(minute, ss.StartUtc, ss.EndUtc) AS decimal(18, 4)) / 60.0 < 0.25 THEN 0.25
                            ELSE CAST(DATEDIFF(minute, ss.StartUtc, ss.EndUtc) AS decimal(18, 4)) / 60.0
                        END, 2) AS decimal(18, 2)),
                    PricePerParticipantCoins = CAST(ROUND(
                        h.PricePerHourCoins *
                        CASE
                            WHEN DATEDIFF(minute, ss.StartUtc, ss.EndUtc) <= 0 THEN 0
                            WHEN CAST(DATEDIFF(minute, ss.StartUtc, ss.EndUtc) AS decimal(18, 4)) / 60.0 < 0.25 THEN 0.25
                            ELSE CAST(DATEDIFF(minute, ss.StartUtc, ss.EndUtc) AS decimal(18, 4)) / 60.0
                        END, 2) AS decimal(18, 2))
                FROM ScheduledSessions ss
                INNER JOIN Halls h ON h.Id = ss.HallId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerParticipantCoins",
                table: "ScheduledSessions");

            migrationBuilder.DropColumn(
                name: "PriceTotalCoins",
                table: "ScheduledSessions");
        }
    }
}
