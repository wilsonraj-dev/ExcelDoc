using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace ExcelDoc.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NomeDocumento = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Endpoint = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Empresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NomeEmpresa = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresa", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Colecoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NomeColecao = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    TipoColecao = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FK_IdEmpresa = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colecoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colecoes_Empresa_FK_IdEmpresa",
                        column: x => x.FK_IdEmpresa,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LinkServiceLayer = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Database = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    UsuarioBanco = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    SenhaBanco = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    UsuarioSAP = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    SenhaSAP = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FK_IdEmpresa = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configuracoes_Empresa_FK_IdEmpresa",
                        column: x => x.FK_IdEmpresa,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NomeUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    SenhaHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    TipoUsuario = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    FK_IdEmpresa = table.Column<int>(type: "int", nullable: true),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Empresa_FK_IdEmpresa",
                        column: x => x.FK_IdEmpresa,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentoColecao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FK_IdDocumento = table.Column<int>(type: "int", nullable: false),
                    FK_IdColecao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoColecao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoColecao_Colecoes_FK_IdColecao",
                        column: x => x.FK_IdColecao,
                        principalTable: "Colecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentoColecao_Documentos_FK_IdDocumento",
                        column: x => x.FK_IdDocumento,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MapeamentoCampos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IndiceColuna = table.Column<int>(type: "int", nullable: false),
                    NomeCampo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    DescricaoCampo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    TipoCampo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Formato = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    FK_IdColecao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapeamentoCampos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapeamentoCampos_Colecoes_FK_IdColecao",
                        column: x => x.FK_IdColecao,
                        principalTable: "Colecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Processamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FK_IdUsuario = table.Column<int>(type: "int", nullable: false),
                    FK_IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    FK_IdDocumento = table.Column<int>(type: "int", nullable: false),
                    NomeArquivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    DataExecucao = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TotalRegistros = table.Column<int>(type: "int", nullable: false),
                    TotalSucesso = table.Column<int>(type: "int", nullable: false),
                    TotalErro = table.Column<int>(type: "int", nullable: false),
                    HashArquivo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Processamento_Documentos_FK_IdDocumento",
                        column: x => x.FK_IdDocumento,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Processamento_Empresa_FK_IdEmpresa",
                        column: x => x.FK_IdEmpresa,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Processamento_Usuario_FK_IdUsuario",
                        column: x => x.FK_IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProcessamentoItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FK_IdProcessamento = table.Column<int>(type: "int", nullable: false),
                    LinhaExcel = table.Column<int>(type: "int", nullable: false),
                    JsonEnviado = table.Column<string>(type: "longtext", nullable: false),
                    JsonRetorno = table.Column<string>(type: "longtext", nullable: true),
                    Erro = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessamentoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessamentoItem_Processamento_FK_IdProcessamento",
                        column: x => x.FK_IdProcessamento,
                        principalTable: "Processamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Colecoes_FK_IdEmpresa",
                table: "Colecoes",
                column: "FK_IdEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_FK_IdEmpresa",
                table: "Configuracoes",
                column: "FK_IdEmpresa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoColecao_FK_IdColecao",
                table: "DocumentoColecao",
                column: "FK_IdColecao");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoColecao_FK_IdDocumento_FK_IdColecao",
                table: "DocumentoColecao",
                columns: new[] { "FK_IdDocumento", "FK_IdColecao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapeamentoCampos_FK_IdColecao",
                table: "MapeamentoCampos",
                column: "FK_IdColecao");

            migrationBuilder.CreateIndex(
                name: "IX_Processamento_FK_IdDocumento",
                table: "Processamento",
                column: "FK_IdDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_Processamento_FK_IdEmpresa_HashArquivo",
                table: "Processamento",
                columns: new[] { "FK_IdEmpresa", "HashArquivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Processamento_FK_IdUsuario",
                table: "Processamento",
                column: "FK_IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessamentoItem_FK_IdProcessamento",
                table: "ProcessamentoItem",
                column: "FK_IdProcessamento");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_FK_IdEmpresa",
                table: "Usuario",
                column: "FK_IdEmpresa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracoes");

            migrationBuilder.DropTable(
                name: "DocumentoColecao");

            migrationBuilder.DropTable(
                name: "MapeamentoCampos");

            migrationBuilder.DropTable(
                name: "ProcessamentoItem");

            migrationBuilder.DropTable(
                name: "Colecoes");

            migrationBuilder.DropTable(
                name: "Processamento");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Empresa");
        }
    }
}
