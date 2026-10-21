namespace ExplainIt.API.Models
{
    public class Profile
    {
        public Guid Id { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}