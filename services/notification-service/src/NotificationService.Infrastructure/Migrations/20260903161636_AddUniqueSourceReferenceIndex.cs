using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSourceReferenceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_SourceReference",
                schema: "notification",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_SourceReference",
                schema: "notification",
                table: "notifications",
                column: "SourceReference",
                unique: true,
                filter: "\"SourceReference\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_SourceReference",
                schema: "notification",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_SourceReference",
                schema: "notification",
                table: "notifications",
                column: "SourceReference");
        }
    }
}
