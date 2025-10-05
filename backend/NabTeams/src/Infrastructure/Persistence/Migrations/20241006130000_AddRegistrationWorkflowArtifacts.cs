using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations;

public partial class AddRegistrationWorkflowArtifacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RegistrationNotifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipantRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                Channel = table.Column<string>(type: "text", nullable: false),
                Recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegistrationNotifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_RegistrationNotifications_ParticipantRegistrations_ParticipantRegistrationId",
                    column: x => x.ParticipantRegistrationId,
                    principalTable: "ParticipantRegistrations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RegistrationPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipantRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                PaymentUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                GatewayReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegistrationPayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_RegistrationPayments_ParticipantRegistrations_ParticipantRegistrationId",
                    column: x => x.ParticipantRegistrationId,
                    principalTable: "ParticipantRegistrations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RegistrationNotifications_ParticipantRegistrationId",
            table: "RegistrationNotifications",
            column: "ParticipantRegistrationId");

        migrationBuilder.CreateIndex(
            name: "IX_RegistrationPayments_ParticipantRegistrationId",
            table: "RegistrationPayments",
            column: "ParticipantRegistrationId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RegistrationNotifications");

        migrationBuilder.DropTable(
            name: "RegistrationPayments");
    }
}
