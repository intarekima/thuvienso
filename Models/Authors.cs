using System.ComponentModel.DataAnnotations;

namespace thuvienso.Models;

public class Author
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; }

    [StringLength(1000), MaxLength(1000)]
    public string? Description { get; set; }

    public ICollection<DocumentAuthor> DocumentAuthors { get; set; } = new List<DocumentAuthor>();
}
