namespace ExplainIt.API.Models
{
    public class DocumentTerm
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public string Term { get; set; } = string.Empty;

        public string SimpleExplanation { get; set; } = string.Empty;

        public Document? Document { get; set; }
    }
}