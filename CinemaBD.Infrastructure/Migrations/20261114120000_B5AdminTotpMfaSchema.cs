using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CinemaBD.Infrastructure.Persistence;

#nullable disable

namespace CinemaBD.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261114120000_B5AdminTotpMfaSchema")]
public partial class B5AdminTotpMfaSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminMfa",
            columns: table => new
            {
                AdminID = table.Column<int>(type: "int", nullable: false),
                SecretProtected = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                Enabled = table.Column<bool>(type: "bit", nullable: false),
                EnabledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastUsedStep = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminMfa", x => x.AdminID);
                table.ForeignKey(
                    name: "FK_AdminMfa_Admin_AdminID",
                    column: x => x.AdminID,
                    principalTable: "Admin",
                    principalColumn: "AdminID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AdminRecoveryCodes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AdminID = table.Column<int>(type: "int", nullable: false),
                CodeHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminRecoveryCodes", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdminRecoveryCodes_Admin_AdminID",
                    column: x => x.AdminID,
                    principalTable: "Admin",
                    principalColumn: "AdminID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminRecoveryCodes_AdminID",
            table: "AdminRecoveryCodes",
            column: "AdminID");

        migrationBuilder.CreateIndex(
            name: "IX_AdminRecoveryCodes_CodeHash",
            table: "AdminRecoveryCodes",
            column: "CodeHash");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AdminRecoveryCodes");

        migrationBuilder.DropTable(
            name: "AdminMfa");
    }
}
