using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleballBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_openplaysessions_open_play_session_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_openplaysessions_clients_client_id",
                table: "openplaysessions");

            migrationBuilder.DropForeignKey(
                name: "FK_openplaysessions_courts_court_id",
                table: "openplaysessions");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "openplaysessions",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "openplaysessions",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "openplaysessions",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "skill_level",
                table: "openplaysessions",
                newName: "SkillLevel");

            migrationBuilder.RenameColumn(
                name: "price_per_player",
                table: "openplaysessions",
                newName: "PricePerPlayer");

            migrationBuilder.RenameColumn(
                name: "max_players",
                table: "openplaysessions",
                newName: "MaxPlayers");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "openplaysessions",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "host_name",
                table: "openplaysessions",
                newName: "HostName");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "openplaysessions",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "current_players",
                table: "openplaysessions",
                newName: "CurrentPlayers");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "openplaysessions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "court_id",
                table: "openplaysessions",
                newName: "CourtId");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "openplaysessions",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_openplaysessions_court_id",
                table: "openplaysessions",
                newName: "IX_openplaysessions_CourtId");

            migrationBuilder.RenameIndex(
                name: "IX_openplaysessions_client_id",
                table: "openplaysessions",
                newName: "IX_openplaysessions_ClientId");

            migrationBuilder.RenameColumn(
                name: "Read",
                table: "notifications",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "open_play_session_id",
                table: "bookings",
                newName: "OpenPlaySessionId");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_open_play_session_id",
                table: "bookings",
                newName: "IX_bookings_OpenPlaySessionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "timeslots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "openplaysessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "openplaysessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "openplaysessions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "clients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentExpiresAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "blockeddates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "blockeddates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "openplaysessioncourts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenPlaySessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourtId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_openplaysessioncourts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_openplaysessioncourts_courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_openplaysessioncourts_openplaysessions_OpenPlaySessionId",
                        column: x => x.OpenPlaySessionId,
                        principalTable: "openplaysessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_openplaysessioncourts_CourtId",
                table: "openplaysessioncourts",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_openplaysessioncourts_OpenPlaySessionId_CourtId",
                table: "openplaysessioncourts",
                columns: new[] { "OpenPlaySessionId", "CourtId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_openplaysessions_OpenPlaySessionId",
                table: "bookings",
                column: "OpenPlaySessionId",
                principalTable: "openplaysessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_openplaysessions_clients_ClientId",
                table: "openplaysessions",
                column: "ClientId",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_openplaysessions_courts_CourtId",
                table: "openplaysessions",
                column: "CourtId",
                principalTable: "courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_openplaysessions_OpenPlaySessionId",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_openplaysessions_clients_ClientId",
                table: "openplaysessions");

            migrationBuilder.DropForeignKey(
                name: "FK_openplaysessions_courts_CourtId",
                table: "openplaysessions");

            migrationBuilder.DropTable(
                name: "openplaysessioncourts");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "openplaysessions");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "openplaysessions",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "openplaysessions",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "openplaysessions",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "SkillLevel",
                table: "openplaysessions",
                newName: "skill_level");

            migrationBuilder.RenameColumn(
                name: "PricePerPlayer",
                table: "openplaysessions",
                newName: "price_per_player");

            migrationBuilder.RenameColumn(
                name: "MaxPlayers",
                table: "openplaysessions",
                newName: "max_players");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "openplaysessions",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "HostName",
                table: "openplaysessions",
                newName: "host_name");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "openplaysessions",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "CurrentPlayers",
                table: "openplaysessions",
                newName: "current_players");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "openplaysessions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CourtId",
                table: "openplaysessions",
                newName: "court_id");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "openplaysessions",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_openplaysessions_CourtId",
                table: "openplaysessions",
                newName: "IX_openplaysessions_court_id");

            migrationBuilder.RenameIndex(
                name: "IX_openplaysessions_ClientId",
                table: "openplaysessions",
                newName: "IX_openplaysessions_client_id");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "notifications",
                newName: "Read");

            migrationBuilder.RenameColumn(
                name: "OpenPlaySessionId",
                table: "bookings",
                newName: "open_play_session_id");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_OpenPlaySessionId",
                table: "bookings",
                newName: "IX_bookings_open_play_session_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "timeslots",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "openplaysessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "openplaysessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "clients",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentExpiresAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "blockeddates",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "blockeddates",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_openplaysessions_open_play_session_id",
                table: "bookings",
                column: "open_play_session_id",
                principalTable: "openplaysessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_openplaysessions_clients_client_id",
                table: "openplaysessions",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_openplaysessions_courts_court_id",
                table: "openplaysessions",
                column: "court_id",
                principalTable: "courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
