namespace ExplainIt.API.Models;
public class DocumentQuestion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Mode { get; set; } = "simple";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document? Document { get; set; }
}
