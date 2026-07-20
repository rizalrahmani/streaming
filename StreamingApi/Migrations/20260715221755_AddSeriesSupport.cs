using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Movies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "EpisodeNumber",
                table: "Movies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeries",
                table: "Movies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Movies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonNumber",
                table: "Movies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ParentId",
                table: "Movies",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Movies_ParentId",
                table: "Movies",
                column: "ParentId",
                principalTable: "Movies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Movies_ParentId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_ParentId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "EpisodeNumber",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "IsSeries",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "SeasonNumber",
                table: "Movies");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Movies",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
