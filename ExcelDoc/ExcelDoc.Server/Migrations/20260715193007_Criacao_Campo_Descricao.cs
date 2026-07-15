using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Criacao_Campo_Descricao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Colecoes",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Colecoes");
        }
    }
}
