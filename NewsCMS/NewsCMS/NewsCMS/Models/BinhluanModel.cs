
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    [Table("BinhLuan")]
    public class BinhluanModel
    {
        [Key]
        public int MaBinhLuan { get; set; }

        [Required]
        public int MaBaiViet { get; set; }

        [Required]
        public int MaDocGia { get; set; }

        [Required]
        [StringLength(2000)]
        public string NoiDung { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime NgayBinhLuan { get; set; }
    }
}
