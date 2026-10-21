using System.Text.Json.Serialization;

namespace ExplainIt.API.Models
{
    public class DocumentCategory
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Prevent Category -> Documents -> Category JSON serialization cycles.
        // API endpoints should still prefer DTOs for predictable responses.
        [JsonIgnore]
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
