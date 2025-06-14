using System;
using System.ComponentModel.DataAnnotations;

namespace thuvienso.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(255), EmailAddress]
        public string Email { get; set; } = null!;

        [MaxLength(12)]
        public string? Phone { get; set; }

        [Required, MaxLength(255)]
        public string Subject { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
