using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_users_ClientId",
                table: "users",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_clients_ClientId",
                table: "users",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_clients_ClientId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ClientId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "users");
        }
    }
}
