using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAdr.Vtn.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<string>(type: "TEXT", nullable: false),
                    EventName = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: true),
                    Targets = table.Column<string>(type: "TEXT", nullable: true),
                    ReportDescriptors = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadDescriptors = table.Column<string>(type: "TEXT", nullable: true),
                    IntervalPeriod = table.Column<string>(type: "TEXT", nullable: true),
                    Intervals = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModificationDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ObjectType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProgramName = table.Column<string>(type: "TEXT", nullable: false),
                    IntervalPeriod = table.Column<string>(type: "TEXT", nullable: true),
                    ProgramDescriptions = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadDescriptors = table.Column<string>(type: "TEXT", nullable: true),
                    Attributes = table.Column<string>(type: "TEXT", nullable: true),
                    Targets = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModificationDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ObjectType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    EventId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientName = table.Column<string>(type: "TEXT", nullable: false),
                    ReportName = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadDescriptors = table.Column<string>(type: "TEXT", nullable: true),
                    Resources = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModificationDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ObjectType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    VenName = table.Column<string>(type: "TEXT", nullable: false),
                    Targets = table.Column<string>(type: "TEXT", nullable: true),
                    Attributes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModificationDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ObjectType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ProgramId",
                table: "Events",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_ProgramName",
                table: "Programs",
                column: "ProgramName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_EventId_ClientName_CreatedDateTime",
                table: "Reports",
                columns: new[] { "EventId", "ClientName", "CreatedDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Vens_VenName",
                table: "Vens",
                column: "VenName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Vens");
        }
    }
}
