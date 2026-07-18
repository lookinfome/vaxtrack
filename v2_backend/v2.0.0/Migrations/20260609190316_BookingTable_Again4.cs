using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v2._0._0.Migrations
{
    /// <inheritdoc />
    public partial class BookingTable_Again4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserUid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dose1RequestedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Dose1SlotNumber = table.Column<int>(type: "int", nullable: false),
                    Dose1HospitalUid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDose1Completed = table.Column<bool>(type: "bit", nullable: false),
                    Dose1CompletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Dose2RequestedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Dose2HospitalUid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDose2Completed = table.Column<bool>(type: "bit", nullable: false),
                    Dose2CompletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVaccinationCompleted = table.Column<bool>(type: "bit", nullable: false),
                    VaccinationCompletedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsD1RequestCanceled = table.Column<bool>(type: "bit", nullable: false),
                    IsD2RequestCanceled = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");
        }
    }
}
