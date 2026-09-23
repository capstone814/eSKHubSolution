using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        public string? Image { get; set; }

        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
        [Required]
        public string Email { get; set; } = default!;

        [Required]
        public string Role { get; set; } = default!;

        [Required]
        public string Fullname { get; set; } = default!;

        [Required]
        public string Position { get; set; } = default!;

        [Required]
        public string Area { get; set; } = default!;
    }
}