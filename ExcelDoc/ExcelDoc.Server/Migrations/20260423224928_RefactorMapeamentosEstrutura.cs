using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMapeamentosEstrutura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mapeamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    FK_IdColecao = table.Column<int>(type: "int", nullable: false),
                    FK_IdEmpresa = table.Column<int>(type: "int", nullable: true),
                    IsPadrao = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mapeamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mapeamento_Colecoes_FK_IdColecao",
                        column: x => x.FK_IdColecao,
                        principalTable: "Colecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mapeamento_Empresa_FK_IdEmpresa",
                        column: x => x.FK_IdEmpresa,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Mapeamento_FK_IdColecao",
                table: "Mapeamento",
                column: "FK_IdColecao");

            migrationBuilder.CreateIndex(
                name: "IX_Mapeamento_FK_IdEmpresa",
                table: "Mapeamento",
                column: "FK_IdEmpresa");

            migrationBuilder.AddColumn<int>(
                name: "FK_IdMapeamento",
                table: "MapeamentoCampos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapeamentoCampos_FK_IdMapeamento",
                table: "MapeamentoCampos",
                column: "FK_IdMapeamento");

            migrationBuilder.Sql(@"
INSERT INTO Mapeamento (Nome, FK_IdColecao, FK_IdEmpresa, IsPadrao, DataCriacao)
SELECT CONCAT('Mapeamento padrão - ', c.NomeColecao), c.Id, c.FK_IdEmpresa, TRUE, UTC_TIMESTAMP()
FROM Colecoes c;");

            migrationBuilder.Sql(@"
UPDATE MapeamentoCampos mc
INNER JOIN Mapeamento m ON m.FK_IdColecao = mc.FK_IdColecao AND m.IsPadrao = TRUE
SET mc.FK_IdMapeamento = m.Id;");

            migrationBuilder.Sql(@"
DELETE duplicateMapeamentoCampos
FROM MapeamentoCampos duplicateMapeamentoCampos
INNER JOIN MapeamentoCampos preservedMapeamentoCampos
    ON duplicateMapeamentoCampos.FK_IdMapeamento = preservedMapeamentoCampos.FK_IdMapeamento
    AND duplicateMapeamentoCampos.IndiceColuna = preservedMapeamentoCampos.IndiceColuna
    AND duplicateMapeamentoCampos.Id > preservedMapeamentoCampos.Id
WHERE duplicateMapeamentoCampos.FK_IdMapeamento IS NOT NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "FK_IdMapeamento",
                table: "MapeamentoCampos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MapeamentoCampos_Mapeamento_FK_IdMapeamento",
                table: "MapeamentoCampos",
                column: "FK_IdMapeamento",
                principalTable: "Mapeamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_MapeamentoCampos_Colecoes_FK_IdColecao",
                table: "MapeamentoCampos");

            migrationBuilder.DropIndex(
                name: "IX_MapeamentoCampos_FK_IdColecao",
                table: "MapeamentoCampos");

            migrationBuilder.DropColumn(
                name: "FK_IdColecao",
                table: "MapeamentoCampos");

            migrationBuilder.CreateIndex(
                name: "UX_MapeamentoCampos_FK_IdMapeamento_IndiceColuna",
                table: "MapeamentoCampos",
                columns: new[] { "FK_IdMapeamento", "IndiceColuna" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MapeamentoCampos_FK_IdMapeamento_IndiceColuna",
                table: "MapeamentoCampos");

            migrationBuilder.DropForeignKey(
                name: "FK_MapeamentoCampos_Mapeamento_FK_IdMapeamento",
                table: "MapeamentoCampos");

            migrationBuilder.AddColumn<int>(
                name: "FK_IdColecao",
                table: "MapeamentoCampos",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE MapeamentoCampos mc
INNER JOIN Mapeamento m ON m.Id = mc.FK_IdMapeamento
SET mc.FK_IdColecao = m.FK_IdColecao;");

            migrationBuilder.AlterColumn<int>(
                name: "FK_IdColecao",
                table: "MapeamentoCampos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapeamentoCampos_FK_IdColecao",
                table: "MapeamentoCampos",
                column: "FK_IdColecao");

            migrationBuilder.AddForeignKey(
                name: "FK_MapeamentoCampos_Colecoes_FK_IdColecao",
                table: "MapeamentoCampos",
                column: "FK_IdColecao",
                principalTable: "Colecoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropIndex(
                name: "IX_MapeamentoCampos_FK_IdMapeamento",
                table: "MapeamentoCampos");

            migrationBuilder.DropColumn(
                name: "FK_IdMapeamento",
                table: "MapeamentoCampos");

            migrationBuilder.DropTable(
                name: "Mapeamento");
        }
    }
}
