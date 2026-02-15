using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BattleQuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "action_types",
                columns: table => new
                {
                    action_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_action_types", x => x.action_type_id);
                });

            migrationBuilder.CreateTable(
                name: "bosses",
                columns: table => new
                {
                    boss_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bosses", x => x.boss_id);
                });

            migrationBuilder.CreateTable(
                name: "channels",
                columns: table => new
                {
                    channel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_channels", x => x.channel_id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "quests",
                columns: table => new
                {
                    quest_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quests", x => x.quest_id);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    zone_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zones", x => x.zone_id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    player_id = table.Column<string>(type: "varchar(20)", nullable: false),
                    last_known_zone_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "varchar(100)", nullable: true),
                    last_known_level = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.player_id);
                    table.ForeignKey(
                        name: "fk_players_zones_last_known_zone_id",
                        column: x => x.last_known_zone_id,
                        principalTable: "zones",
                        principalColumn: "zone_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    action_type_id = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<string>(type: "varchar(20)", nullable: true),
                    victim_player_id = table.Column<string>(type: "varchar(20)", nullable: true),
                    killer_player_id = table.Column<string>(type: "varchar(20)", nullable: true),
                    quest_id = table.Column<string>(type: "varchar(20)", nullable: true),
                    zone_id = table.Column<int>(type: "integer", nullable: true),
                    item_id = table.Column<int>(type: "integer", nullable: true),
                    boss_id = table.Column<int>(type: "integer", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    xp = table.Column<int>(type: "integer", nullable: true),
                    gold = table.Column<int>(type: "integer", nullable: true),
                    hp = table.Column<int>(type: "integer", nullable: true),
                    damage = table.Column<int>(type: "integer", nullable: true),
                    method = table.Column<string>(type: "varchar(100)", nullable: true),
                    player_level = table.Column<int>(type: "integer", nullable: true),
                    points = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "varchar(255)", nullable: true),
                    location_x = table.Column<int>(type: "integer", nullable: true),
                    location_y = table.Column<int>(type: "integer", nullable: true),
                    message_text = table.Column<string>(type: "text", nullable: true),
                    inserted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    raw = table.Column<string>(type: "text", nullable: false),
                    event_hash = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.event_id);
                    table.ForeignKey(
                        name: "fk_events_action_types_action_type_id",
                        column: x => x.action_type_id,
                        principalTable: "action_types",
                        principalColumn: "action_type_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_bosses_boss_id",
                        column: x => x.boss_id,
                        principalTable: "bosses",
                        principalColumn: "boss_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "channels",
                        principalColumn: "channel_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_players_killer_player_id",
                        column: x => x.killer_player_id,
                        principalTable: "players",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_players_victim_player_id",
                        column: x => x.victim_player_id,
                        principalTable: "players",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_quests_quest_id",
                        column: x => x.quest_id,
                        principalTable: "quests",
                        principalColumn: "quest_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "zone_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_action_types_name",
                table: "action_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bosses_name",
                table: "bosses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_channels_name",
                table: "channels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_action_type_id_item_id",
                table: "events",
                columns: new[] { "action_type_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_boss_id",
                table: "events",
                column: "boss_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_channel_id",
                table: "events",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_hash",
                table: "events",
                column: "event_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_item_id",
                table: "events",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_killer_player_id_action_type_id",
                table: "events",
                columns: new[] { "killer_player_id", "action_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_occurred_at",
                table: "events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_events_player_id_action_type_id",
                table: "events",
                columns: new[] { "player_id", "action_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_quest_id",
                table: "events",
                column: "quest_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_victim_player_id_action_type_id",
                table: "events",
                columns: new[] { "victim_player_id", "action_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_zone_id",
                table: "events",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_name",
                table: "items",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_players_last_known_zone_id",
                table: "players",
                column: "last_known_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_players_name",
                table: "players",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_zones_name",
                table: "zones",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "action_types");

            migrationBuilder.DropTable(
                name: "bosses");

            migrationBuilder.DropTable(
                name: "channels");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "quests");

            migrationBuilder.DropTable(
                name: "zones");
        }
    }
}
