using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v2._0._0.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectionFlagsToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsD1RejectedByAdmin",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsD2RejectedByAdmin",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsD1RejectedByAdmin",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsD2RejectedByAdmin",
                table: "Bookings");
        }
    }
}
