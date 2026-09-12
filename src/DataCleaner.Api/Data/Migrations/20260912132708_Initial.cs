using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataCleaner.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_batches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_batch_id = table.Column<int>(type: "integer", nullable: false),
                    match_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duplicate_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_duplicate_groups_import_batches_import_batch_id",
                        column: x => x.import_batch_id,
                        principalTable: "import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_batch_id = table.Column<int>(type: "integer", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    raw_full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    raw_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    raw_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    raw_birth_date = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    raw_city = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    middle_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    city = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    issues = table.Column<int>(type: "integer", nullable: false),
                    duplicate_group_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_records_duplicate_groups_duplicate_group_id",
                        column: x => x.duplicate_group_id,
                        principalTable: "duplicate_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_customer_records_import_batches_import_batch_id",
                        column: x => x.import_batch_id,
                        principalTable: "import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_records_duplicate_group_id",
                table: "customer_records",
                column: "duplicate_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_records_import_batch_id_email",
                table: "customer_records",
                columns: new[] { "import_batch_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_records_import_batch_id_phone",
                table: "customer_records",
                columns: new[] { "import_batch_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_groups_import_batch_id",
                table: "duplicate_groups",
                column: "import_batch_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_records");

            migrationBuilder.DropTable(
                name: "duplicate_groups");

            migrationBuilder.DropTable(
                name: "import_batches");
        }
    }
}
