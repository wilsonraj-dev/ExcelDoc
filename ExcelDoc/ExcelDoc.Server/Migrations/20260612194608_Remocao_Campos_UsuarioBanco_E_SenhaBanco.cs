using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Remocao_Campos_UsuarioBanco_E_SenhaBanco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenhaBanco",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "UsuarioBanco",
                table: "Configuracoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenhaBanco",
                table: "Configuracoes",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioBanco",
                table: "Configuracoes",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
