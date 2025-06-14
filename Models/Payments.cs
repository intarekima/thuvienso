using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models;

public class Payment
{
    public int Id { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey("Document")]
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    [Column(TypeName = "DECIMAL(5,2)")]
    public decimal PercentPaid { get; set; } = 0.00m;

    [Column(TypeName = "DECIMAL(10,2)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "DECIMAL(10,2)")]
    public decimal PricePaid { get; set; }

    [Column(TypeName = "ENUM('pending','paid','failed')")]
    public string PaymentStatus { get; set; } = "pending";

    [MaxLength(100)]
    public string OrderCode { get; set; } = null!;

    public string? QrCodeUrl { get; set; }
    public string? CheckoutUrl { get; set; }

    public DateTime? TransactionTime { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
