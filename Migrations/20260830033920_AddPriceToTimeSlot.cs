using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceToTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "pricerules",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_pricerules_ClientId",
                table: "pricerules",
                newName: "IX_pricerules_client_id");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "courts",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_courts_ClientId",
                table: "courts",
                newName: "IX_courts_client_id");

            migrationBuilder.RenameColumn(
                name: "Subdomain",
                table: "clients",
                newName: "subdomain");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "clients",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "clients",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "clients",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PrimaryColor",
                table: "clients",
                newName: "primary_color");

            migrationBuilder.RenameColumn(
                name: "LogoUrl",
                table: "clients",
                newName: "logo_url");

            migrationBuilder.RenameColumn(
                name: "GcashNumber",
                table: "clients",
                newName: "gcash_number");

            migrationBuilder.RenameColumn(
                name: "GcashAccountName",
                table: "clients",
                newName: "gcash_account_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "clients",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AccentColor",
                table: "clients",
                newName: "accent_color");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "bookings",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_ClientId",
                table: "bookings",
                newName: "IX_bookings_client_id");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "blockeddates",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_blockeddates_ClientId",
                table: "blockeddates",
                newName: "IX_blockeddates_client_id");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "timeslots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "courts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PeakPricePerHour",
                table: "courts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_blockeddates_clients_client_id",
                table: "blockeddates",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_clients_client_id",
                table: "bookings",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_courts_clients_client_id",
                table: "courts",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pricerules_clients_client_id",
                table: "pricerules",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blockeddates_clients_client_id",
                table: "blockeddates");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_clients_client_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_courts_clients_client_id",
                table: "courts");

            migrationBuilder.DropForeignKey(
                name: "FK_pricerules_clients_client_id",
                table: "pricerules");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "timeslots");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "courts");

            migrationBuilder.DropColumn(
                name: "PeakPricePerHour",
                table: "courts");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "pricerules",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_pricerules_client_id",
                table: "pricerules",
                newName: "IX_pricerules_ClientId");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "courts",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_courts_client_id",
                table: "courts",
                newName: "IX_courts_ClientId");

            migrationBuilder.RenameColumn(
                name: "subdomain",
                table: "clients",
                newName: "Subdomain");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "clients",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "clients",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "clients",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "primary_color",
                table: "clients",
                newName: "PrimaryColor");

            migrationBuilder.RenameColumn(
                name: "logo_url",
                table: "clients",
                newName: "LogoUrl");

            migrationBuilder.RenameColumn(
                name: "gcash_number",
                table: "clients",
                newName: "GcashNumber");

            migrationBuilder.RenameColumn(
                name: "gcash_account_name",
                table: "clients",
                newName: "GcashAccountName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "clients",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "accent_color",
                table: "clients",
                newName: "AccentColor");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "bookings",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_client_id",
                table: "bookings",
                newName: "IX_bookings_ClientId");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "blockeddates",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_blockeddates_client_id",
                table: "blockeddates",
                newName: "IX_blockeddates_ClientId");

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
    }
}
