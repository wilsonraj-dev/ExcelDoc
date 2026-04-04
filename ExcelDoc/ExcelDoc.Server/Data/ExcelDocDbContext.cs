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

        public DbSet<MapeamentoCampo> MapeamentoCampos => Set<MapeamentoCampo>();

        public DbSet<Processamento> Processamentos => Set<Processamento>();

        public DbSet<ProcessamentoItem> ProcessamentoItens => Set<ProcessamentoItem>();

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

                entity.Property(e => e.UsuarioBanco)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.SenhaBanco)
                    .IsRequired()
                    .HasMaxLength(500);

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

                entity.HasIndex(e => new { e.FK_IdDocumento, e.FK_IdColecao })
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

                entity.HasOne(e => e.Colecao)
                    .WithMany(e => e.MapeamentoCampos)
                    .HasForeignKey(e => e.FK_IdColecao)
                    .OnDelete(DeleteBehavior.Cascade);
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

                entity.HasIndex(e => new { e.FK_IdEmpresa, e.HashArquivo })
                    .IsUnique();

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
