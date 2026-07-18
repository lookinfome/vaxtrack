using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace v2._0._0.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCredentialsSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserCredentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserCredentials",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserCredentials");
        }
    }
}
