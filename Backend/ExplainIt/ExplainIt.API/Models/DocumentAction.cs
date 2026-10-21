namespace ExplainIt.API.Models
{
    public class DocumentAction
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime? Deadline { get; set; }

        public Document? Document { get; set; }
    }
}