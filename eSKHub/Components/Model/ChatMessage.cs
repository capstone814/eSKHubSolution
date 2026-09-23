using System;
using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public string SenderUsername { get; set; } = default!;

        [Required]
        public string ReceiverUsername { get; set; } = default!;

        [Required]
        public string Message { get; set; } = default!;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }
    }
}
