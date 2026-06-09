using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v1Remastered.Migrations
{
    /// <inheritdoc />
    public partial class UserDetailsModelUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserPassword",
                table: "UserDetails");

            migrationBuilder.DropColumn(
                name: "UserVaccinationId",
                table: "UserDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserPassword",
                table: "UserDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserVaccinationId",
                table: "UserDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
