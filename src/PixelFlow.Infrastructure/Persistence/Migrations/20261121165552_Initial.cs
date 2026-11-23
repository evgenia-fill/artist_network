using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_images", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "image_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    parent_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_image_versions_image_versions_parent_version_id",
                        column: x => x.parent_version_id,
                        principalTable: "image_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_image_versions_images_image_id",
                        column: x => x.image_id,
                        principalTable: "images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_image_versions_image_id_number",
                table: "image_versions",
                columns: new[] { "image_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_image_versions_parent_version_id",
                table: "image_versions",
                column: "parent_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_images_owner_id",
                table: "images",
                column: "owner_id");
            
            migrationBuilder.Sql(@"ALTER TABLE images
    ADD CONSTRAINT fk_images_current_version
    FOREIGN KEY (current_version_id) REFERENCES image_versions (id)
    DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE images DROP CONSTRAINT fk_images_current_version;");
            migrationBuilder.DropTable(
                name: "image_versions");

            migrationBuilder.DropTable(
                name: "images");
        }
    }
}
