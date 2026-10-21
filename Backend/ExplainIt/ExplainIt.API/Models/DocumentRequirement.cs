namespace ExplainIt.API.Models
{
    public class DocumentRequirement
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool Completed { get; set; } = false;

        public Document? Document { get; set; }
    }
}