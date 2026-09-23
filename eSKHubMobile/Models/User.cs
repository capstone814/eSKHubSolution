using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace eSKHubMobile.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        public string? Image { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; }
        public string? Fullname { get; set; }
        public string? Position { get; set; }
        public string? Area { get; set; }
    }
}
