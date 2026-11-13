using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CinemaBD.Infrastructure.Persistence;

#nullable disable

namespace CinemaBD.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261113110000_B3bSessionDeviceMetadata")]
public partial class B3bSessionDeviceMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DeviceName",
            table: "IdentitySessions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "UserAgent",
            table: "IdentitySessions",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DeviceName", table: "IdentitySessions");
        migrationBuilder.DropColumn(name: "UserAgent", table: "IdentitySessions");
    }
}
