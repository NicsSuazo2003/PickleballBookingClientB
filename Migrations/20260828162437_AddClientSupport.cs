using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClientSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "pricerules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "courts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "blockeddates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Subdomain = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: false),
                    AccentColor = table.Column<string>(type: "text", nullable: false),
                    GcashNumber = table.Column<string>(type: "text", nullable: true),
                    GcashAccountName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pricerules_ClientId",
                table: "pricerules",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_courts_ClientId",
                table: "courts",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_ClientId",
                table: "bookings",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_blockeddates_ClientId",
                table: "blockeddates",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_blockeddates_clients_ClientId",
                table: "blockeddates",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_clients_ClientId",
                table: "bookings",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_courts_clients_ClientId",
                table: "courts",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pricerules_clients_ClientId",
                table: "pricerules",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blockeddates_clients_ClientId",
                table: "blockeddates");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_clients_ClientId",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_courts_clients_ClientId",
                table: "courts");

            migrationBuilder.DropForeignKey(
                name: "FK_pricerules_clients_ClientId",
                table: "pricerules");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropIndex(
                name: "IX_pricerules_ClientId",
                table: "pricerules");

            migrationBuilder.DropIndex(
                name: "IX_courts_ClientId",
                table: "courts");

            migrationBuilder.DropIndex(
                name: "IX_bookings_ClientId",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_blockeddates_ClientId",
                table: "blockeddates");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "pricerules");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "courts");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "blockeddates");
        }
    }
}
