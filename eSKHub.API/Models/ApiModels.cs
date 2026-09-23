using System.ComponentModel.DataAnnotations;

namespace eSKHub.API.Models
{
    // Request/Response DTOs
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Fullname { get; set; }
        public string? Position { get; set; }
        public string? Area { get; set; }
        public string? Image { get; set; }
    }

    public class AnnouncementDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Author { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
    }

    public class SyncResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AnnouncementDto> Announcements { get; set; } = new();
        public List<EventDto> Events { get; set; } = new();
        public List<ProgramProjectDto> Programs { get; set; } = new();
        public List<NewsArticleDto> News { get; set; } = new();
        public List<PollDto> Polls { get; set; } = new();
        public DateTime SyncTimestamp { get; set; }
    }

    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? Organizer { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class ProgramProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Venue { get; set; }
        public decimal Budget { get; set; }
        public string? TargetBeneficiaries { get; set; }
        public string? Image { get; set; }
    }

    public class NewsArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string NewsType { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? Author { get; set; }
        public string? Image { get; set; }
    }

    public class PollDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Options { get; set; } = string.Empty;
        public string? Votes { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateExpires { get; set; }
        public bool IsActive { get; set; }
        public string? VoterIds { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    // Database Models
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? Position { get; set; }
        public string? Area { get; set; }
        public string? Role { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }

    public class Announcement
    {
        public int Id { get; set; }
        [Required]
        public string Subject { get; set; } = string.Empty;
        [Required]
        public string Body { get; set; } = string.Empty;
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateModified { get; set; }
        public bool IsPublished { get; set; } = false;
        public string? Author { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
        public byte[]? AttachmentData { get; set; }
        public string? AttachmentMimeType { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        [Required]
        public string Location { get; set; } = string.Empty;
        [Required]
        public string EventType { get; set; } = string.Empty;
        public string? Organizer { get; set; }
        public string? OrganizerPosition { get; set; }
        public string? OrganizerArea { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public bool IsPublished { get; set; } = false;
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class NewsArticle
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Summary { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        [Required]
        public string NewsType { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public bool IsPublished { get; set; } = false;
        public string? Author { get; set; }
        public string? AuthorArea { get; set; }
        public string? AuthorPosition { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
        public byte[]? AttachmentData { get; set; }
        public string? AttachmentMimeType { get; set; }
    }

    public class ProgramProject
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Venue { get; set; }
        public decimal Budget { get; set; }
        public string? TargetBeneficiaries { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public bool IsPublished { get; set; } = false;
        public string? Objectives { get; set; }
        public string? ExpectedOutcomes { get; set; }
        public string? Timeline { get; set; }
        public string? ProposerPosition { get; set; }
        public string? ProposerArea { get; set; }
        public string? Partners { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
        public string? ProposedBy { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
        public byte[]? AttachmentData { get; set; }
        public string? AttachmentMimeType { get; set; }
    }

    public class Poll
    {
        public int Id { get; set; }
        [Required]
        public string Question { get; set; } = string.Empty;
        [Required]
        public string Options { get; set; } = string.Empty;
        public string? Votes { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateExpires { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
        public string? CreatorArea { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
        public string? VoterIds { get; set; }
    }

    public class Feedback
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        public string? Subject { get; set; }
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime DateSubmitted { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }

    public class Area
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Type { get; set; } = string.Empty; // Municipality, Barangay
    }
}