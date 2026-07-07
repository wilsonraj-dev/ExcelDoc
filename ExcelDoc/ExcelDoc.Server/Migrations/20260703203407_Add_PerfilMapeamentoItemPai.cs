using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Add_PerfilMapeamentoItemPai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FK_IdPerfilMapeamentoItemPai",
                table: "PerfilMapeamentoItem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilMapeamentoItem_FK_IdPerfilMapeamentoItemPai",
                table: "PerfilMapeamentoItem",
                column: "FK_IdPerfilMapeamentoItemPai");

            migrationBuilder.AddForeignKey(
                name: "FK_PerfilMapeamentoItem_PerfilMapeamentoItemPai",
                table: "PerfilMapeamentoItem",
                column: "FK_IdPerfilMapeamentoItemPai",
                principalTable: "PerfilMapeamentoItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerfilMapeamentoItem_PerfilMapeamentoItemPai",
                table: "PerfilMapeamentoItem");

            migrationBuilder.DropIndex(
                name: "IX_PerfilMapeamentoItem_FK_IdPerfilMapeamentoItemPai",
                table: "PerfilMapeamentoItem");

            migrationBuilder.DropColumn(
                name: "FK_IdPerfilMapeamentoItemPai",
                table: "PerfilMapeamentoItem");
        }
    }
}
