using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    public partial class AlignTopicProposalDay2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurposeCode",
                table: "ExternalPaymentRecords",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "COIN_PURCHASE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HallReviews_RatingStars",
                table: "HallReviews",
                sql: "[RatingStars] >= 1 AND [RatingStars] <= 5");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HallReviews_RatingStars",
                table: "HallReviews");

            migrationBuilder.DropColumn(
                name: "PurposeCode",
                table: "ExternalPaymentRecords");
        }
    }
}

