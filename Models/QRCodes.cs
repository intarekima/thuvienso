using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace thuvienso.Models
{
    public class QRCode
    {
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [Required]
        [EnumDataType(typeof(QRCodeType))]
        public QRCodeType Type { get; set; }

        [StringLength(500)]
        public string? QrUrl { get; set; }

        public int ScanCount { get; set; } = 0;

        [ForeignKey("DocumentId")]
        public Document Document { get; set; } = null!;
    }

    public enum QRCodeType
    {
        view,
        download
    }

    public static class QRCodeFileName
    {
        public const string View = "qr-view.png";
        public const string Download = "qr-download.png";
    }
}
