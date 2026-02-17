using System.ComponentModel.DataAnnotations;

namespace SharkyParser.Api.Data.Models;

public class FileRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    [MaxLength(50)]
    public string LogType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string? AnalysisResult { get; set; }
}
