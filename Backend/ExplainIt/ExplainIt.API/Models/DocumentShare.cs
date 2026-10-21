namespace ExplainIt.API.Models;
public class DocumentShare
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public Document? Document { get; set; }
}
