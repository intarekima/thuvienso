using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MaxLength(255)]
    public string Password { get; set; }

    [MaxLength(6)]
    public string? ResetCode { get; set; }

    public DateTime? ResetCodeExpiry { get; set; }

    [Required]
    [Column(TypeName = "ENUM('admin','user')")]
    public string Role { get; set; } = "user";

    // ======= NAVIGATION PROPERTIES =======

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Download> Downloads { get; set; } = new List<Download>();
}
