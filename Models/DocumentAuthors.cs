using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models;

public class DocumentAuthor
{
    public int Id { get; set; }

    [ForeignKey("Document")]
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    [ForeignKey("Author")]
    public int AuthorId { get; set; }
    public Author Author { get; set; }
}
