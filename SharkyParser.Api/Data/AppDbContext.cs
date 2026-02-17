using Microsoft.EntityFrameworkCore;
using SharkyParser.Api.Data.Models;

namespace SharkyParser.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FileRecord> Files { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasIndex(e => e.UploadedAt);
            entity.Property(e => e.AnalysisResult)
                  .HasColumnType("jsonb");
        });
    }
}
