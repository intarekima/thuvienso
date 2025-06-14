using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; }

    [StringLength(1000), MaxLength(1000)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Category? Parent { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Document> Documents { get; set; } = new List<Document>();

}
