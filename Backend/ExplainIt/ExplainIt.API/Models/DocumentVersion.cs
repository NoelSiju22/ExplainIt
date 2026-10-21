namespace ExplainIt.API.Models;
public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document? Document { get; set; }
}
