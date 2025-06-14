using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace thuvienso.Models
{
    public class Publisher
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [StringLength(1000), MaxLength(1000)]
        public string? Description { get; set; }

        public ICollection<Document> Documents { get; set; }
    }
}
