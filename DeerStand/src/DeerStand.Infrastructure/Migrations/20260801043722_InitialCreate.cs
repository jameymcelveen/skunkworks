using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeerStand.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clubs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clubs", x => x.id);
                    table.ForeignKey(
                        name: "FK_clubs_profiles_owner_id",
                        column: x => x.owner_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "club_members",
                columns: table => new
                {
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_club_members", x => new { x.club_id, x.profile_id });
                    table.ForeignKey(
                        name: "FK_club_members_clubs_club_id",
                        column: x => x.club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_club_members_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stands", x => x.id);
                    table.ForeignKey(
                        name: "FK_stands_clubs_club_id",
                        column: x => x.club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "active_check_ins",
                columns: table => new
                {
                    stand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_active_check_ins", x => x.stand_id);
                    table.ForeignKey(
                        name: "FK_active_check_ins_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_active_check_ins_stands_stand_id",
                        column: x => x.stand_id,
                        principalTable: "stands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    stand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    log_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_activity_logs_clubs_club_id",
                        column: x => x.club_id,
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_activity_logs_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_logs_stands_stand_id",
                        column: x => x.stand_id,
                        principalTable: "stands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "check_in_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    checked_out_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_check_in_history_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_check_in_history_stands_stand_id",
                        column: x => x.stand_id,
                        principalTable: "stands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_active_check_ins_profile_id",
                table: "active_check_ins",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_club_id",
                table: "activity_logs",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_profile_id",
                table: "activity_logs",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_stand_id",
                table: "activity_logs",
                column: "stand_id");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_history_profile_id",
                table: "check_in_history",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_history_stand_id",
                table: "check_in_history",
                column: "stand_id");

            migrationBuilder.CreateIndex(
                name: "IX_club_members_profile_id",
                table: "club_members",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_clubs_invite_code",
                table: "clubs",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clubs_owner_id",
                table: "clubs",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_stands_club_id",
                table: "stands",
                column: "club_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "active_check_ins");

            migrationBuilder.DropTable(
                name: "activity_logs");

            migrationBuilder.DropTable(
                name: "check_in_history");

            migrationBuilder.DropTable(
                name: "club_members");

            migrationBuilder.DropTable(
                name: "stands");

            migrationBuilder.DropTable(
                name: "clubs");

            migrationBuilder.DropTable(
                name: "profiles");
        }
    }
}
