using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiTheftApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSecurityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSecurityEvent",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSecurityEventAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSecurityEvent",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastSecurityEventAt",
                table: "Devices");
        }
    }
}
