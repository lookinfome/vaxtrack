using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v2._0._0.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Tables_User_And_Hospital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HospitalUid",
                table: "Hospitals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HospitalUid",
                table: "Hospitals");
        }
    }
}
