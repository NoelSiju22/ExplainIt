using ExplainIt.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ExplainIt.API.Data
{
    public class ExplainItDbContext : DbContext
    {
        public ExplainItDbContext(
            DbContextOptions<ExplainItDbContext> options)
            : base(options)
        {
        }

        // Tables

        public DbSet<Profile> Profiles { get; set; }

        public DbSet<DocumentCategory> DocumentCategories { get; set; }

        public DbSet<Document> Documents { get; set; }

        public DbSet<DocumentAction> DocumentActions { get; set; }

        public DbSet<DocumentDeadline> DocumentDeadlines { get; set; }

        public DbSet<DocumentRequirement> DocumentRequirements { get; set; }

        public DbSet<DocumentTerm> DocumentTerms { get; set; }

        public DbSet<DocumentNote> DocumentNotes { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<DocumentTag> DocumentTags { get; set; }
        public DbSet<DocumentEvent> DocumentEvents { get; set; }
        public DbSet<DocumentReminder> DocumentReminders { get; set; }
        public DbSet<DocumentShare> DocumentShares { get; set; }
        public DbSet<DocumentQuestion> DocumentQuestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------------------
            // Profile
            // -----------------------------------------

            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .ValueGeneratedNever();

                entity.Property(p => p.FullName)
                    .HasMaxLength(150);

                entity.Property(p => p.Email)
                    .HasMaxLength(255);
            });

            // -----------------------------------------
            // DocumentCategory
            // -----------------------------------------

            modelBuilder.Entity<DocumentCategory>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Description)
                    .HasMaxLength(500);
            });

            // -----------------------------------------
            // Document
            // -----------------------------------------

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(d => d.FilePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(d => d.Description)
                    .HasMaxLength(2000);

                entity.Property(d => d.Status).IsRequired().HasMaxLength(50);
                entity.Property(d => d.SimpleSummary).HasMaxLength(4000);
                entity.Property(d => d.WhatIsIt).HasMaxLength(4000);
                entity.Property(d => d.WhyReceived).HasMaxLength(4000);
                entity.Property(d => d.WhatToDo).HasMaxLength(4000);
                entity.Property(d => d.Consequences).HasMaxLength(4000);
                entity.Property(d => d.SourceText);

                // Document -> Profile
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Documents)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Document -> Category
                entity.HasOne(d => d.Category)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // -----------------------------------------
            // DocumentAction
            // -----------------------------------------

            modelBuilder.Entity<DocumentAction>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(a => a.Description)
                    .HasMaxLength(2000);

                entity.Property(a => a.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(a => a.Document)
                    .WithMany(d => d.Actions)
                    .HasForeignKey(a => a.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -----------------------------------------
            // DocumentDeadline
            // -----------------------------------------

            modelBuilder.Entity<DocumentDeadline>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasOne(d => d.Document)
                    .WithMany(doc => doc.Deadlines)
                    .HasForeignKey(d => d.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -----------------------------------------
            // DocumentRequirement
            // -----------------------------------------

            modelBuilder.Entity<DocumentRequirement>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(r => r.Description)
                    .HasMaxLength(2000);

                entity.HasOne(r => r.Document)
                    .WithMany(d => d.Requirements)
                    .HasForeignKey(r => r.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -----------------------------------------
            // DocumentTerm
            // -----------------------------------------

            modelBuilder.Entity<DocumentTerm>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Term)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(t => t.SimpleExplanation)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(t => t.Document)
                    .WithMany(d => d.Terms)
                    .HasForeignKey(t => t.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -----------------------------------------
            // DocumentNote
            // -----------------------------------------

            modelBuilder.Entity<DocumentNote>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Content)
                    .IsRequired()
                    .HasMaxLength(5000);

                entity.HasOne(n => n.Document)
                    .WithMany(d => d.Notes)
                    .HasForeignKey(n => n.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            // -----------------------------------------
            // ExplainIt expansion tables
            // -----------------------------------------
            modelBuilder.Entity<DocumentVersion>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Title).IsRequired().HasMaxLength(255);
                entity.Property(v => v.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(v => v.Description).HasMaxLength(2000);
                entity.Property(v => v.ChangeSummary).HasMaxLength(1000);
                entity.HasOne(v => v.Document).WithMany(d => d.Versions).HasForeignKey(v => v.DocumentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
            });
            modelBuilder.Entity<DocumentTag>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(80);
                entity.HasOne(t => t.Document).WithMany(d => d.Tags).HasForeignKey(t => t.DocumentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(t => new { t.DocumentId, t.Name }).IsUnique();
            });
            modelBuilder.Entity<DocumentEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(80);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.HasOne(e => e.Document).WithMany(d => d.Events).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DocumentReminder>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Title).IsRequired().HasMaxLength(255);
                entity.HasOne(r => r.Document).WithMany(d => d.Reminders).HasForeignKey(r => r.DocumentId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DocumentShare>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Token).IsRequired().HasMaxLength(120);
                entity.HasIndex(s => s.Token).IsUnique();
                entity.HasOne(s => s.Document).WithMany(d => d.Shares).HasForeignKey(s => s.DocumentId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DocumentQuestion>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Question).IsRequired().HasMaxLength(2000);
                entity.Property(q => q.Answer).IsRequired().HasMaxLength(10000);
                entity.Property(q => q.Mode).IsRequired().HasMaxLength(30);
                entity.HasOne(q => q.Document).WithMany(d => d.Questions).HasForeignKey(q => q.DocumentId).OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
