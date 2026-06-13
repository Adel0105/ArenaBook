using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HallReviewSessionUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HallReviews_UserId",
                table: "HallReviews");

            migrationBuilder.CreateIndex(
                name: "IX_HallReviews_UserId_ScheduledSessionId",
                table: "HallReviews",
                columns: new[] { "UserId", "ScheduledSessionId" },
                unique: true,
                filter: "[ScheduledSessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HallReviews_UserId_ScheduledSessionId",
                table: "HallReviews");

            migrationBuilder.CreateIndex(
                name: "IX_HallReviews_UserId",
                table: "HallReviews",
                column: "UserId");
        }
    }
}
