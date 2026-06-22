using System;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Model;

namespace LibraryAPI.Data
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true; // Soft Delete
    }
    
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) 
        : base(options) { }

        public DbSet<LivroModelo> Books { get; set; }
        public DbSet<AutorModelo> Authors { get; set; }
        public DbSet<BookAuthor> BookAuthors { get; set; }
        public DbSet<UsuarioModelo> Users { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<ClienteModelo> Clientes { get; set; }
        public DbSet<ClienteAccessLog> ClienteAccessLogs { get; set; }
         

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração do relacionamento N:N Books-Authors
            modelBuilder.Entity<BookAuthor>()
                .HasKey(ba => new { ba.BookId, ba.AuthorId });

            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Book)
                .WithMany(b => b.BookAuthors)
                .HasForeignKey(ba => ba.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Author)
                .WithMany(a => a.BookAuthors)
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuração do Loan
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.User)
                .WithMany(u => u.Loans)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Book)
                .WithMany(b => b.Loans)
                .HasForeignKey(l => l.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices para melhorar performance de busca
            modelBuilder.Entity<LivroModelo>()
                .HasIndex(b => b.ISBN)
                .IsUnique();

            modelBuilder.Entity<LivroModelo>()
                .HasIndex(b => b.Title);

            modelBuilder.Entity<UsuarioModelo>()
                .HasIndex(u => u.Email)
                .IsUnique();

             // Configuração da tabela de Clientes
            modelBuilder.Entity<ClienteModelo>(entity =>
            {
                entity.HasIndex(c => c.ClientId)
                    .IsUnique();
                
                entity.HasIndex(c => c.Nome);
                
                entity.HasQueryFilter(c => c.IsActive);
            });
            
            // Configuração dos logs de acesso
            modelBuilder.Entity<ClienteAccessLog>(entity =>
            {
                entity.HasIndex(l => l.ClienteId);
                entity.HasIndex(l => l.Timestamp);
                
                entity.HasOne(l => l.Cliente)
                    .WithMany(c => c.AccessLogs)
                    .HasForeignKey(l => l.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Filtro global para Soft Delete
            modelBuilder.Entity<LivroModelo>().HasQueryFilter(b => b.IsActive);
            modelBuilder.Entity<AutorModelo>().HasQueryFilter(a => a.IsActive);
            modelBuilder.Entity<UsuarioModelo>().HasQueryFilter(u => u.IsActive);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Atualizar timestamps automaticamente
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && 
                        (e.State == EntityState.Added || e.State == EntityState.Modified)))
            {
                if (entry.State == EntityState.Added)
                    ((BaseEntity)entry.Entity).CreatedAt = DateTime.UtcNow;
                else
                    ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}