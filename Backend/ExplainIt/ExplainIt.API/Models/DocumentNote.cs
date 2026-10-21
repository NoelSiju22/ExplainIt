namespace ExplainIt.API.Models
{
    public class DocumentNote
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Document? Document { get; set; }
    }
}