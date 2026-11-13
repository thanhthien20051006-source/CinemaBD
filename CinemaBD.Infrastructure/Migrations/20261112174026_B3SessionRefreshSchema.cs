using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class B3SessionRefreshSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Password lifecycle columns and RevokedTokens already exist in the
            // SQL bootstrap schema; do not recreate them in this delta migration.
            migrationBuilder.CreateTable(
                name: "IdentitySessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentitySessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAudit",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAudit", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    RefreshTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByRefreshTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.RefreshTokenId);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_IdentitySessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "IdentitySessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByRefreshTokenId",
                        column: x => x.ReplacedByRefreshTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "RefreshTokenId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentitySessions_AccountType_AccountId_RevokedAtUtc",
                table: "IdentitySessions",
                columns: new[] { "AccountType", "AccountId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentitySessions_ExpiresAtUtc",
                table: "IdentitySessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByRefreshTokenId",
                table: "RefreshTokens",
                column: "ReplacedByRefreshTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId_ExpiresAtUtc",
                table: "RefreshTokens",
                columns: new[] { "SessionId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAudit_EventType_OccurredAtUtc",
                table: "SecurityAudit",
                columns: new[] { "EventType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAudit_OccurredAtUtc",
                table: "SecurityAudit",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RefreshTokens");
            migrationBuilder.DropTable(name: "SecurityAudit");
            migrationBuilder.DropTable(name: "IdentitySessions");
        }
    }
}
