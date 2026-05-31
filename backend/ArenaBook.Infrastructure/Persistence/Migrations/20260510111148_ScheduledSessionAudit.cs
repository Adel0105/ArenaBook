using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaBook.Infrastructure.Persistence.Migrations
{
    public partial class ScheduledSessionAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledSessionAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FromLifecycleStatusId = table.Column<int>(type: "int", nullable: true),
                    ToLifecycleStatusId = table.Column<int>(type: "int", nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledSessionAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledSessionAuditEntries_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessionAuditEntries_ActorUserId",
                table: "ScheduledSessionAuditEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessionAuditEntries_OccurredUtc",
                table: "ScheduledSessionAuditEntries",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessionAuditEntries_SessionId",
                table: "ScheduledSessionAuditEntries",
                column: "SessionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledSessionAuditEntries");
        }
    }
}

