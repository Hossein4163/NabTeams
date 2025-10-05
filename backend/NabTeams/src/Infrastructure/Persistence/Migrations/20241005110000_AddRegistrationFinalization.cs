using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizedAt",
                table: "ParticipantRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ParticipantRegistrations",
                type: "text",
                nullable: false,
                defaultValue: "Submitted");

            migrationBuilder.AddColumn<string>(
                name: "SummaryFileUrl",
                table: "ParticipantRegistrations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizedAt",
                table: "JudgeRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "JudgeRegistrations",
                type: "text",
                nullable: false,
                defaultValue: "Submitted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizedAt",
                table: "InvestorRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InvestorRegistrations",
                type: "text",
                nullable: false,
                defaultValue: "Submitted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "ParticipantRegistrations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ParticipantRegistrations");

            migrationBuilder.DropColumn(
                name: "SummaryFileUrl",
                table: "ParticipantRegistrations");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "JudgeRegistrations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JudgeRegistrations");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "InvestorRegistrations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InvestorRegistrations");
        }
    }
}
