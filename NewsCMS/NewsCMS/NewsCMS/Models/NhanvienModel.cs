using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    [Table("NhanVien")]
    public class NhanVienModel
    {
        [Key]
        public int MaNhanVien { get; set; }

        [Required]
        public int MaTaiKhoan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(10, MinimumLength = 10,
            ErrorMessage = "Số điện thoại phải có đúng 10 chữ số.")]
        [RegularExpression(@"^(?!0{10}$)0[0-9]{9}$",
            ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0, gồm 10 chữ số và không được là dãy toàn số 0.")]
        public string SoDienThoai { get; set; } = string.Empty;

        public DateTime? NgayVaoLam { get; set; }
    }
}