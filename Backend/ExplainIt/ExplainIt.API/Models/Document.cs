namespace ExplainIt.API.Models
{
    public class Document
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public int CategoryId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool IsImportant { get; set; } = false;

        public DateTime? DocumentDate { get; set; }

        // ExplainIt intelligence and lifecycle fields
        public string Status { get; set; } = "Needs attention";
        public string? SimpleSummary { get; set; }
        public string? WhatIsIt { get; set; }
        public string? WhyReceived { get; set; }
        public string? WhatToDo { get; set; }
        public string? Consequences { get; set; }
        public string? SourceText { get; set; }
        public DateTime? LastViewedAt { get; set; }
        public DateTime? DeletedAt { get; set; }


        // Relationships

        public Profile? User { get; set; }

        public DocumentCategory? Category { get; set; }

        public ICollection<DocumentAction> Actions { get; set; } = new List<DocumentAction>();

        public ICollection<DocumentDeadline> Deadlines { get; set; } = new List<DocumentDeadline>();

        public ICollection<DocumentRequirement> Requirements { get; set; } = new List<DocumentRequirement>();

        public ICollection<DocumentTerm> Terms { get; set; } = new List<DocumentTerm>();

        public ICollection<DocumentNote> Notes { get; set; } = new List<DocumentNote>();
        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
        public ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
        public ICollection<DocumentEvent> Events { get; set; } = new List<DocumentEvent>();
        public ICollection<DocumentReminder> Reminders { get; set; } = new List<DocumentReminder>();
        public ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
        public ICollection<DocumentQuestion> Questions { get; set; } = new List<DocumentQuestion>();
    }
}