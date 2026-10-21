namespace ExplainIt.API.Models
{
    public class DocumentDeadline
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime Deadline { get; set; }

        public bool IsCompleted { get; set; } = false;

        public Document? Document { get; set; }
    }
}