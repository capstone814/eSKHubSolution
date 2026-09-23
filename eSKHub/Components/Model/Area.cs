using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class Area
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        [Required]
        public string Type { get; set; } = default!;
    }
}