using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class B6DurableAccountLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "Admin",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndUtc",
                table: "Admin",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "KHACHHANG",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndUtc",
                table: "KHACHHANG",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AccessFailedCount", table: "Admin");
            migrationBuilder.DropColumn(name: "LockoutEndUtc", table: "Admin");
            migrationBuilder.DropColumn(name: "AccessFailedCount", table: "KHACHHANG");
            migrationBuilder.DropColumn(name: "LockoutEndUtc", table: "KHACHHANG");
        }
    }
}
