
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    [Table("TaiKhoan")]
    public class TaiKhoanModel
    {
        [Key]
        public int MaTaiKhoan { get; set; }
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
        [Column(TypeName = "nvarchar(50)")]
        public string TenDangNhap { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email không được để trống")]
        [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Column(TypeName = "nvarchar(254)")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(500, ErrorMessage = "Mật khẩu tối đa 500 ký tự")]
        [Column(TypeName = "nvarchar(500)")]
        public string MatKhau { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mã vai trò không được để trống")]
        public int MaVaiTro { get; set; }
        [Required]
        public bool TrangThai { get; set; }
        [Required]
        [Column(TypeName = "datetime2(7)")]
        public DateTime NgayTao { get; set; }
    }
}

