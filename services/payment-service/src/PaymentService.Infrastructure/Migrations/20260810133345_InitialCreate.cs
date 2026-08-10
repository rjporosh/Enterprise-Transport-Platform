using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProviderPaymentId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_refunds",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderRefundReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_refunds_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "payment",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOnUtc",
                schema: "payment",
                table: "outbox_messages",
                column: "OccurredOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc",
                schema: "payment",
                table: "outbox_messages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_RetryCount",
                schema: "payment",
                table: "outbox_messages",
                column: "RetryCount");

            migrationBuilder.CreateIndex(
                name: "IX_payment_refunds_CreatedAtUtc",
                schema: "payment",
                table: "payment_refunds",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_refunds_PaymentId",
                schema: "payment",
                table: "payment_refunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_refunds_Status",
                schema: "payment",
                table: "payment_refunds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_refunds_TenantId",
                schema: "payment",
                table: "payment_refunds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CreatedAtUtc",
                schema: "payment",
                table: "payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CustomerId",
                schema: "payment",
                table: "payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_IdempotencyKey",
                schema: "payment",
                table: "payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_OrderReference",
                schema: "payment",
                table: "payments",
                column: "OrderReference");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ProviderPaymentId",
                schema: "payment",
                table: "payments",
                column: "ProviderPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                schema: "payment",
                table: "payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId",
                schema: "payment",
                table: "payments",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "payment_refunds",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "payment");
        }
    }
}
