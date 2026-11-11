using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    [Table("LichSuDuyet")]
    public class LichSuDuyetModel
    {
        [Key]
        public int MaLichSu { get; set; }

        [Required]
        public int MaBaiViet { get; set; }

        [Required]
        public int MaNguoiDuyet { get; set; }

        [Required]
        [StringLength(30)]
        public string HanhDong { get; set; }

        [StringLength(1000)]
        public string? GhiChu { get; set; }

        [Required]
        public DateTime ThoiGian { get; set; }
    }
}