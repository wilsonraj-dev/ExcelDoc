using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Add_Idioma_Usuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Idioma",
                table: "Usuario",
                type: "varchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Idioma",
                table: "Usuario");
        }
    }
}
