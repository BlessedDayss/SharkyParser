using Microsoft.EntityFrameworkCore;
using SharkyParser.Api.Data.Models;

namespace SharkyParser.Api.Data.Repositories;

public class FileRepository(AppDbContext db) : IFileRepository
{
    public async Task AddAsync(FileRecord record, CancellationToken ct = default)
    {
        db.Files.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FileRecord>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        return await db.Files
            .OrderByDescending(f => f.UploadedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}
