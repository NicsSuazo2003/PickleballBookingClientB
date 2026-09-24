using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserFromBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_UserId",
                table: "bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "bookings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_UserId",
                table: "bookings",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_UserId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_UserId",
                table: "bookings",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
