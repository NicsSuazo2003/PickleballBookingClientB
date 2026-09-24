using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCourtSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourtId",
                table: "timeslots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourtId",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CourtId",
                table: "blockeddates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_timeslots_CourtId",
                table: "timeslots",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_CourtId",
                table: "bookings",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_blockeddates_CourtId",
                table: "blockeddates",
                column: "CourtId");

            migrationBuilder.AddForeignKey(
                name: "FK_blockeddates_courts_CourtId",
                table: "blockeddates",
                column: "CourtId",
                principalTable: "courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_courts_CourtId",
                table: "bookings",
                column: "CourtId",
                principalTable: "courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timeslots_courts_CourtId",
                table: "timeslots",
                column: "CourtId",
                principalTable: "courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blockeddates_courts_CourtId",
                table: "blockeddates");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_courts_CourtId",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_timeslots_courts_CourtId",
                table: "timeslots");

            migrationBuilder.DropIndex(
                name: "IX_timeslots_CourtId",
                table: "timeslots");

            migrationBuilder.DropIndex(
                name: "IX_bookings_CourtId",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_blockeddates_CourtId",
                table: "blockeddates");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "timeslots");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "blockeddates");
        }
    }
}
