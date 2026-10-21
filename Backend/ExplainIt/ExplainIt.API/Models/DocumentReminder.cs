namespace ExplainIt.API.Models;
public class DocumentReminder
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? DeadlineId { get; set; }
    public Guid? ActionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime RemindAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document? Document { get; set; }
}
