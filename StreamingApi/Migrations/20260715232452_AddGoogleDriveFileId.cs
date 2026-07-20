using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleDriveFileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleDriveFileId",
                table: "Movies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleDriveFileId",
                table: "Movies");
        }
    }
}
