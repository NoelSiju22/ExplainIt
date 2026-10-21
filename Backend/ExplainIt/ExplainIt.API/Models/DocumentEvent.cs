namespace ExplainIt.API.Models;
public class DocumentEvent
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document? Document { get; set; }
}
