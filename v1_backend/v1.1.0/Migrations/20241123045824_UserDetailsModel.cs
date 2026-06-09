using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v1Remastered.Migrations
{
    /// <inheritdoc />
    public partial class UserDetailsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDetails",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    UserPassword = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    UserBirthdate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserUid = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    UserGender = table.Column<string>(type: "TEXT", nullable: false),
                    UserPhone = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    UserRole = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserVaccinationId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDetails", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDetails");
        }
    }
}
