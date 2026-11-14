using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CinemaBD.Infrastructure.Persistence;

#nullable disable

namespace CinemaBD.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261114000000_B3cSessionLastSeenIp")]
public partial class B3cSessionLastSeenIp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LastSeenIp",
            table: "IdentitySessions",
            type: "nvarchar(45)",
            maxLength: 45,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LastSeenIp", table: "IdentitySessions");
    }
}
