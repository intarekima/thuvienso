using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models;

public class Download
{
    public int Id { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }
    public User User { get; set; }

    [ForeignKey("Document")]
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
}
