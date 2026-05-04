using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilMapeamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerfilMapeamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    FK_IdDocumento = table.Column<int>(type: "int", nullable: false),
                    FK_IdEmpresa = table.Column<int>(type: "int", nullable: true),
                    IsPadrao = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilMapeamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilMapeamento_Documentos_FK_IdDocumento",
                        column: x => x.FK_IdDocumento,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilMapeamento_Empresa_FK_IdEmpresa",
                        column: x => x.FK_IdEmpresa,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PerfilMapeamentoItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FK_IdPerfilMapeamento = table.Column<int>(type: "int", nullable: false),
                    FK_IdColecao = table.Column<int>(type: "int", nullable: false),
                    FK_IdMapeamento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilMapeamentoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilMapeamentoItem_Colecoes_FK_IdColecao",
                        column: x => x.FK_IdColecao,
                        principalTable: "Colecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilMapeamentoItem_Mapeamento_FK_IdMapeamento",
                        column: x => x.FK_IdMapeamento,
                        principalTable: "Mapeamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilMapeamentoItem_PerfilMapeamento_FK_IdPerfilMapeamento",
                        column: x => x.FK_IdPerfilMapeamento,
                        principalTable: "PerfilMapeamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilMapeamento_FK_IdDocumento",
                table: "PerfilMapeamento",
                column: "FK_IdDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilMapeamento_FK_IdEmpresa",
                table: "PerfilMapeamento",
                column: "FK_IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilMapeamentoItem_FK_IdColecao",
                table: "PerfilMapeamentoItem",
                column: "FK_IdColecao");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilMapeamentoItem_FK_IdMapeamento",
                table: "PerfilMapeamentoItem",
                column: "FK_IdMapeamento");

            migrationBuilder.CreateIndex(
                name: "UX_PerfilMapeamentoItem_FK_IdPerfilMapeamento_FK_IdColecao",
                table: "PerfilMapeamentoItem",
                columns: new[] { "FK_IdPerfilMapeamento", "FK_IdColecao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfilMapeamentoItem");

            migrationBuilder.DropTable(
                name: "PerfilMapeamento");
        }
    }
}
