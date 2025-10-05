using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations;

public partial class AddBusinessPlanReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessPlanReviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipantRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                OverallScore = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                Summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Strengths = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Risks = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Recommendations = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                RawResponse = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SourceDocumentUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessPlanReviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessPlanReviews_ParticipantRegistrations_ParticipantRegistrationId",
                    column: x => x.ParticipantRegistrationId,
                    principalTable: "ParticipantRegistrations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessPlanReviews_ParticipantRegistrationId",
            table: "BusinessPlanReviews",
            column: "ParticipantRegistrationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BusinessPlanReviews");
    }
}
