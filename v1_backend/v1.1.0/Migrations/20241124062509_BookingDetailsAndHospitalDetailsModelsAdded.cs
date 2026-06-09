using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v1Remastered.Migrations
{
    /// <inheritdoc />
    public partial class BookingDetailsAndHospitalDetailsModelsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingDetails",
                columns: table => new
                {
                    BookingId = table.Column<string>(type: "TEXT", nullable: false),
                    Dose1BookDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Dose2BookDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    D1HospitalId = table.Column<string>(type: "TEXT", nullable: false),
                    D2HospitalId = table.Column<string>(type: "TEXT", nullable: false),
                    D1SlotNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    D2SlotNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Dose1ApproveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Dose2ApproveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    UserVaccinationId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDetails", x => x.BookingId);
                });

            migrationBuilder.CreateTable(
                name: "HospitalDetails",
                columns: table => new
                {
                    HospitalId = table.Column<string>(type: "TEXT", nullable: false),
                    HospitalName = table.Column<string>(type: "TEXT", nullable: false),
                    HospitalAvailableSlots = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalDetails", x => x.HospitalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingDetails");

            migrationBuilder.DropTable(
                name: "HospitalDetails");
        }
    }
}
