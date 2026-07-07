using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Tratamento_Documentos_Sucesso_E_Erro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataExecucao",
                table: "ProcessamentoItem",
                type: "datetime(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFinalizacao",
                table: "ProcessamentoItem",
                type: "datetime(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdDocumentoUnico",
                table: "ProcessamentoItem",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdExcel",
                table: "ProcessamentoItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mensagem",
                table: "ProcessamentoItem",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalIgnorado",
                table: "Processamento",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessamentoItem_IdDocumentoUnico_Status",
                table: "ProcessamentoItem",
                columns: new[] { "IdDocumentoUnico", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessamentoItem_IdDocumentoUnico_Status",
                table: "ProcessamentoItem");

            migrationBuilder.DropColumn(
                name: "DataExecucao",
                table: "ProcessamentoItem");

            migrationBuilder.DropColumn(
                name: "DataFinalizacao",
                table: "ProcessamentoItem");

            migrationBuilder.DropColumn(
                name: "IdDocumentoUnico",
                table: "ProcessamentoItem");

            migrationBuilder.DropColumn(
                name: "IdExcel",
                table: "ProcessamentoItem");

            migrationBuilder.DropColumn(
                name: "Mensagem",
                table: "ProcessamentoItem");

            migrationBuilder.DropColumn(
                name: "TotalIgnorado",
                table: "Processamento");
        }
    }
}
