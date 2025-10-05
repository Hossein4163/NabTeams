using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations;

public partial class AddEventTaskManager : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AiTaskManagerEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "EventId",
            table: "ParticipantRegistrations",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateTable(
            name: "ParticipantTasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipantRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AssignedTo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                AiRecommendation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParticipantTasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_ParticipantTasks_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ParticipantTasks_ParticipantRegistrations_ParticipantRegistrationId",
                    column: x => x.ParticipantRegistrationId,
                    principalTable: "ParticipantRegistrations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantRegistrations_EventId",
            table: "ParticipantRegistrations",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantTasks_EventId",
            table: "ParticipantTasks",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantTasks_ParticipantRegistrationId",
            table: "ParticipantTasks",
            column: "ParticipantRegistrationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ParticipantTasks");

        migrationBuilder.DropTable(
            name: "Events");

        migrationBuilder.DropColumn(
            name: "EventId",
            table: "ParticipantRegistrations");
    }
}
