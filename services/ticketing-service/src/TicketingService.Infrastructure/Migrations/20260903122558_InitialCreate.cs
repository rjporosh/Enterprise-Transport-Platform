using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ticketing");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "ticketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Consumer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.Id, x.Consumer });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "ticketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_templates",
                schema: "ticketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BrandName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryColorHex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    AccentColorHex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    LogoPngBase64 = table.Column<string>(type: "text", nullable: true),
                    TermsText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FooterText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "ticketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    OriginCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinationCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartureUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArrivalUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BusPlateNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BusType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrintCount = table.Column<int>(type: "integer", nullable: false),
                    PdfBytes = table.Column<byte[]>(type: "bytea", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_seats",
                schema: "ticketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PassengerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_seats_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "ticketing",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc_OccurredOnUtc",
                schema: "ticketing",
                table: "outbox_messages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_seats_TicketId",
                schema: "ticketing",
                table: "ticket_seats",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_templates_OperatorId_IsDefault",
                schema: "ticketing",
                table: "ticket_templates",
                columns: new[] { "OperatorId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_BookingId",
                schema: "ticketing",
                table: "tickets",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CustomerId",
                schema: "ticketing",
                table: "tickets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_Number",
                schema: "ticketing",
                table: "tickets",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_VerificationCode",
                schema: "ticketing",
                table: "tickets",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "ticket_seats",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "ticket_templates",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "tickets",
                schema: "ticketing");
        }
    }
}
