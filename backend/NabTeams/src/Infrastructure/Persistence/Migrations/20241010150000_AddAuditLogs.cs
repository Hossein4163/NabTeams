using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations;

public partial class AddAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ActorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CreatedAt",
            table: "AuditLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EntityType_EntityId",
            table: "AuditLogs",
            columns: new[] { "EntityType", "EntityId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLogs");
    }
}
