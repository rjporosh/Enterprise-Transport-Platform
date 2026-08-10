using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSoftDeleteAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "bus",
                table: "depots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "bus",
                table: "depots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "bus",
                table: "depots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "bus",
                table: "depots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "bus",
                table: "depots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "bus",
                table: "depots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "bus",
                table: "buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "bus",
                table: "buses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "bus",
                table: "buses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "bus",
                table: "buses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "bus",
                table: "buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "bus",
                table: "buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_depots_CompanyId",
                schema: "bus",
                table: "depots",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_depots_OrganizationId",
                schema: "bus",
                table: "depots",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_depots_TenantId",
                schema: "bus",
                table: "depots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_depots_TenantId_IsDeleted",
                schema: "bus",
                table: "depots",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_buses_CompanyId",
                schema: "bus",
                table: "buses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_buses_OrganizationId",
                schema: "bus",
                table: "buses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_buses_TenantId",
                schema: "bus",
                table: "buses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_buses_TenantId_IsDeleted",
                schema: "bus",
                table: "buses",
                columns: new[] { "TenantId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_depots_CompanyId",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropIndex(
                name: "IX_depots_OrganizationId",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropIndex(
                name: "IX_depots_TenantId",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropIndex(
                name: "IX_depots_TenantId_IsDeleted",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropIndex(
                name: "IX_buses_CompanyId",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropIndex(
                name: "IX_buses_OrganizationId",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropIndex(
                name: "IX_buses_TenantId",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropIndex(
                name: "IX_buses_TenantId_IsDeleted",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "bus",
                table: "depots");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "bus",
                table: "buses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "bus",
                table: "buses");
        }
    }
}
