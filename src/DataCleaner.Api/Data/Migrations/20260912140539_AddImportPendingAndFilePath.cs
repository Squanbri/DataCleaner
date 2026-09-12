using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataCleaner.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportPendingAndFilePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "file_path",
                table: "import_batches",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_path",
                table: "import_batches");
        }
    }
}
