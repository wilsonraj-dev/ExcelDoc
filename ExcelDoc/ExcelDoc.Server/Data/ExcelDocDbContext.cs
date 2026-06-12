using ExcelDoc.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Data
{
    public class ExcelDocDbContext : DbContext
    {
        public ExcelDocDbContext(DbContextOptions<ExcelDocDbContext> options)
            : base(options)
        {
        }

        public DbSet<Colecao> Colecoes => Set<Colecao>();

        public DbSet<Configuracao> Configuracoes => Set<Configuracao>();

        public DbSet<Documento> Documentos => Set<Documento>();

        public DbSet<DocumentoColecao> DocumentoColecoes => Set<DocumentoColecao>();

        public DbSet<Empresa> Empresas => Set<Empresa>();

        public DbSet<Mapeamento> Mapeamentos => Set<Mapeamento>();

        public DbSet<MapeamentoCampo> MapeamentoCampos => Set<MapeamentoCampo>();

        public DbSet<Processamento> Processamentos => Set<Processamento>();

        public DbSet<ProcessamentoItem> ProcessamentoItens => Set<ProcessamentoItem>();

        public DbSet<PerfilMapeamento> PerfilMapeamentos => Set<PerfilMapeamento>();

        public DbSet<PerfilMapeamentoItem> PerfilMapeamentoItens => Set<PerfilMapeamentoItem>();

        public DbSet<Usuario> Usuarios => Set<Usuario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.ToTable("Empresa");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.NomeEmpresa)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.NomeUsuario)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.SenhaHash)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Email)
                    .HasMaxLength(200);

                entity.Property(e => e.ResetPasswordCode)
                    .HasMaxLength(20);

                entity.Property(e => e.ResetPasswordCodeExpiresAtUtc)
                    .HasPrecision(0);

                entity.Property(e => e.TipoUsuario)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.Property(e => e.Ativo)
                    .HasDefaultValue(true);

                entity.HasOne(e => e.Empresa)
                    .WithMany(e => e.Usuarios)
                    .HasForeignKey(e => e.FK_IdEmpresa)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Configuracao>(entity =>
            {
                entity.ToTable("Configuracoes");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.LinkServiceLayer)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Database)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UsuarioSAP)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.SenhaSAP)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasIndex(e => e.FK_IdEmpresa)
                    .IsUnique();

                entity.HasOne(e => e.Empresa)
                    .WithOne(e => e.Configuracao)
                    .HasForeignKey<Configuracao>(e => e.FK_IdEmpresa)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Documento>(entity =>
            {
                entity.ToTable("Documentos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.NomeDocumento)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Endpoint)
                    .IsRequired()
                    .HasMaxLength(300);
            });

            modelBuilder.Entity<Colecao>(entity =>
            {
                entity.ToTable("Colecoes");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.FK_IdEmpresa)
                    .HasDatabaseName("IX_Colecoes_FK_IdEmpresa");

                entity.Property(e => e.NomeColecao)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.TipoColecao)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(e => e.Empresa)
                    .WithMany(e => e.Colecoes)
                    .HasForeignKey(e => e.FK_IdEmpresa)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DocumentoColecao>(entity =>
            {
                entity.ToTable("DocumentoColecao");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.FK_IdColecao)
                    .HasDatabaseName("IX_DocumentoColecao_FK_IdColecao");

                entity.HasIndex(e => new { e.FK_IdDocumento, e.FK_IdColecao })
                    .HasDatabaseName("IX_DocumentoColecao_FK_IdDocumento_FK_IdColecao")
                    .IsUnique();

                entity.HasOne(e => e.Documento)
                    .WithMany(e => e.DocumentoColecoes)
                    .HasForeignKey(e => e.FK_IdDocumento)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Colecao)
                    .WithMany(e => e.DocumentoColecoes)
                    .HasForeignKey(e => e.FK_IdColecao)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Mapeamento>(entity =>
            {
                entity.ToTable("Mapeamento");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nome)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DataCriacao)
                    .HasPrecision(0);

                entity.Property(e => e.IsPadrao)
                    .HasDefaultValue(false);

                entity.HasIndex(e => e.FK_IdColecao)
                    .HasDatabaseName("IX_Mapeamento_FK_IdColecao");

                entity.HasIndex(e => e.FK_IdEmpresa)
                    .HasDatabaseName("IX_Mapeamento_FK_IdEmpresa");

                entity.HasOne(e => e.Colecao)
                    .WithMany(e => e.Mapeamentos)
                    .HasForeignKey(e => e.FK_IdColecao)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Mapeamento_Colecoes_FK_IdColecao");

                entity.HasOne(e => e.Empresa)
                    .WithMany(e => e.Mapeamentos)
                    .HasForeignKey(e => e.FK_IdEmpresa)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Mapeamento_Empresa_FK_IdEmpresa");
            });

            modelBuilder.Entity<MapeamentoCampo>(entity =>
            {
                entity.ToTable("MapeamentoCampos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.NomeCampo)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DescricaoCampo)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.TipoCampo)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(e => e.Formato)
                    .HasMaxLength(50);                

                entity.HasIndex(e => e.FK_IdMapeamento)
                    .HasDatabaseName("IX_MapeamentoCampos_FK_IdMapeamento");

                entity.HasIndex(e => new { e.FK_IdMapeamento, e.IndiceColuna })
                    .IsUnique()
                    .HasDatabaseName("UX_MapeamentoCampos_FK_IdMapeamento_IndiceColuna");

                entity.HasOne(e => e.Mapeamento)
                    .WithMany(e => e.Campos)
                    .HasForeignKey(e => e.FK_IdMapeamento)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_MapeamentoCampos_Mapeamento_FK_IdMapeamento");
            });

            modelBuilder.Entity<Processamento>(entity =>
            {
                entity.ToTable("Processamento");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.NomeArquivo)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(e => e.HashArquivo)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.DataExecucao)
                    .HasPrecision(0);

                entity.HasOne(e => e.Usuario)
                    .WithMany(e => e.Processamentos)
                    .HasForeignKey(e => e.FK_IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Empresa)
                    .WithMany(e => e.Processamentos)
                    .HasForeignKey(e => e.FK_IdEmpresa)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Documento)
                    .WithMany(e => e.Processamentos)
                    .HasForeignKey(e => e.FK_IdDocumento)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PerfilMapeamento)
                    .WithMany()
                    .HasForeignKey(e => e.FK_IdPerfilMapeamento)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            modelBuilder.Entity<PerfilMapeamento>(entity =>
            {
                entity.ToTable("PerfilMapeamento");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nome)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DataCriacao)
                    .HasPrecision(0);

                entity.Property(e => e.IsPadrao)
                    .HasDefaultValue(false);

                entity.HasIndex(e => e.FK_IdDocumento)
                    .HasDatabaseName("IX_PerfilMapeamento_FK_IdDocumento");

                entity.HasIndex(e => e.FK_IdEmpresa)
                    .HasDatabaseName("IX_PerfilMapeamento_FK_IdEmpresa");

                entity.HasOne(e => e.Documento)
                    .WithMany(e => e.PerfilMapeamentos)
                    .HasForeignKey(e => e.FK_IdDocumento)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PerfilMapeamento_Documentos_FK_IdDocumento");

                entity.HasOne(e => e.Empresa)
                    .WithMany(e => e.PerfilMapeamentos)
                    .HasForeignKey(e => e.FK_IdEmpresa)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PerfilMapeamento_Empresa_FK_IdEmpresa");
            });

            modelBuilder.Entity<PerfilMapeamentoItem>(entity =>
            {
                entity.ToTable("PerfilMapeamentoItem");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.FK_IdPerfilMapeamento, e.FK_IdColecao })
                    .IsUnique()
                    .HasDatabaseName("UX_PerfilMapeamentoItem_FK_IdPerfilMapeamento_FK_IdColecao");

                entity.HasIndex(e => e.FK_IdMapeamento)
                    .HasDatabaseName("IX_PerfilMapeamentoItem_FK_IdMapeamento");

                entity.HasOne(e => e.PerfilMapeamento)
                    .WithMany(e => e.Itens)
                    .HasForeignKey(e => e.FK_IdPerfilMapeamento)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_PerfilMapeamentoItem_PerfilMapeamento_FK_IdPerfilMapeamento");

                entity.HasOne(e => e.Colecao)
                    .WithMany()
                    .HasForeignKey(e => e.FK_IdColecao)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PerfilMapeamentoItem_Colecoes_FK_IdColecao");

                entity.HasOne(e => e.Mapeamento)
                    .WithMany()
                    .HasForeignKey(e => e.FK_IdMapeamento)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PerfilMapeamentoItem_Mapeamento_FK_IdMapeamento");
            });

            modelBuilder.Entity<ProcessamentoItem>(entity =>
            {
                entity.ToTable("ProcessamentoItem");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.JsonEnviado)
                    .IsRequired();

                entity.Property(e => e.JsonRetorno);

                entity.Property(e => e.Erro)
                    .HasMaxLength(4000);

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(e => e.Processamento)
                    .WithMany(e => e.Itens)
                    .HasForeignKey(e => e.FK_IdProcessamento)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
