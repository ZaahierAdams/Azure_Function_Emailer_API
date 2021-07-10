using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EmailerAPI.Repository.Models
{
    public partial class TestdbContext : DbContext
    {
        public TestdbContext()
        {
        }

        public TestdbContext(DbContextOptions<TestdbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Consumer> Consumer { get; set; }
        public virtual DbSet<EmailRecord> EmailRecord { get; set; }
        public virtual DbSet<EmailTable> EmailTable { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("!!!_DATABASE_CONNECTION_STRING_HERE_!!!");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.3-servicing-35854");

            modelBuilder.Entity<Consumer>(entity =>
            {
                entity.Property(e => e.ConsumerIdentifier).IsUnicode(false);

                entity.Property(e => e.ConsumerName).IsUnicode(false);

                entity.Property(e => e.ConsumerShortname).IsUnicode(false);

                entity.Property(e => e.Password).IsUnicode(false);
            });

            modelBuilder.Entity<EmailRecord>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Consumer)
                    .WithMany(p => p.EmailRecord)
                    .HasForeignKey(d => d.ConsumerId)
                    .HasConstraintName("FK_EmailRecord_Consumer");
            });

            modelBuilder.Entity<EmailTable>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });
        }
    }
}
