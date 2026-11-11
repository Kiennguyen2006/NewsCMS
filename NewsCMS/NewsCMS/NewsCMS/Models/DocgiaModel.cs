
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    [Table("DocGia")]
    public class DocgiaModel
    {
        [Key]
        public int MaDocGia { get; set; }

        public int MaTaiKhoan { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; }

        [Required]
        public DateTime NgayDangKy { get; set; }
    }
}
