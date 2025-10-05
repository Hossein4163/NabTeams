using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations;

public partial class AddIntegrationSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IntegrationSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "text", nullable: false),
                ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Configuration = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IntegrationSettings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IntegrationSettings_Type_ProviderKey",
            table: "IntegrationSettings",
            columns: new[] { "Type", "ProviderKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IntegrationSettings_Type_Active",
            table: "IntegrationSettings",
            column: "Type",
            filter: "\"IsActive\" = TRUE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IntegrationSettings");
    }
}
