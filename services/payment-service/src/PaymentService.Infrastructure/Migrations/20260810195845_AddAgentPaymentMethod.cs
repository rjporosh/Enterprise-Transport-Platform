using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_payment_methods",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodType = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_payment_methods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_payment_methods_AgentId",
                schema: "payment",
                table: "agent_payment_methods",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_payment_methods_AgentId_Provider_AccountNumber",
                schema: "payment",
                table: "agent_payment_methods",
                columns: new[] { "AgentId", "Provider", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_payment_methods_IsVerified",
                schema: "payment",
                table: "agent_payment_methods",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_agent_payment_methods_Provider",
                schema: "payment",
                table: "agent_payment_methods",
                column: "Provider");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_payment_methods",
                schema: "payment");
        }
    }
}
