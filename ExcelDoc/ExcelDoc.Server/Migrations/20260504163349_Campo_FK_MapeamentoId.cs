using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Campo_FK_MapeamentoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FK_IdMapeamento",
                table: "Processamento",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Processamento_FK_IdMapeamento",
                table: "Processamento",
                column: "FK_IdMapeamento");

            migrationBuilder.AddForeignKey(
                name: "FK_Processamento_Mapeamento_FK_IdMapeamento",
                table: "Processamento",
                column: "FK_IdMapeamento",
                principalTable: "Mapeamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processamento_Mapeamento_FK_IdMapeamento",
                table: "Processamento");

            migrationBuilder.DropIndex(
                name: "IX_Processamento_FK_IdMapeamento",
                table: "Processamento");

            migrationBuilder.DropColumn(
                name: "FK_IdMapeamento",
                table: "Processamento");
        }
    }
}
