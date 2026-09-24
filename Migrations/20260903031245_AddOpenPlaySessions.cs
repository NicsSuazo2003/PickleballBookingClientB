using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenPlaySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_clients_ClientId",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "users",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_ClientId",
                table: "users",
                newName: "IX_users_client_id");

            migrationBuilder.AddColumn<string>(
                name: "payment_methods",
                table: "clients",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "open_play_session_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "openplaysessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    court_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    current_players = table.Column<int>(type: "integer", nullable: false),
                    price_per_player = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    skill_level = table.Column<string>(type: "text", nullable: false),
                    host_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_openplaysessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_openplaysessions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_openplaysessions_courts_court_id",
                        column: x => x.court_id,
                        principalTable: "courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_open_play_session_id",
                table: "bookings",
                column: "open_play_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_openplaysessions_client_id",
                table: "openplaysessions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_openplaysessions_court_id",
                table: "openplaysessions",
                column: "court_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_openplaysessions_open_play_session_id",
                table: "bookings",
                column: "open_play_session_id",
                principalTable: "openplaysessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_users_clients_client_id",
                table: "users",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_openplaysessions_open_play_session_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_users_clients_client_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "openplaysessions");

            migrationBuilder.DropIndex(
                name: "IX_bookings_open_play_session_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "payment_methods",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "open_play_session_id",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "users",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_users_client_id",
                table: "users",
                newName: "IX_users_ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_clients_ClientId",
                table: "users",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
