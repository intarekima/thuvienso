using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [StringLength(1000), MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        public string? PreviewFileUrl { get; set; }

        [MaxLength(255)]
        public string? Thumb { get; set; }

        // Liên kết danh mục
        [ForeignKey("Category")]
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Liên kết nhà xuất bản
        [ForeignKey("Publisher")]
        public int? PublisherId { get; set; }
        public Publisher? Publisher { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public bool IsFree { get; set; } = true;
        public int? View { get; set; }
        public int? Download { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<DocumentAuthor> DocumentAuthors { get; set; } = new List<DocumentAuthor>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Download> Downloads { get; set; } = new List<Download>();
        public ICollection<QRCode> QRCodes { get; set; } = new List<QRCode>();
    }
}
