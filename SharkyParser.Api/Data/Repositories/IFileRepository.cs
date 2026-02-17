using SharkyParser.Api.Data.Models;

namespace SharkyParser.Api.Data.Repositories;

public interface IFileRepository
{
    Task AddAsync(FileRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<FileRecord>> GetRecentAsync(int count, CancellationToken ct = default);
}
