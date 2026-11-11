
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    public class BaiVietModel
    {
        [Key]
        public int MaBaiViet { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [StringLength(250, MinimumLength = 5,
            ErrorMessage = "Tiêu đề phải từ 5 đến 250 ký tự")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tóm tắt bài viết")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "Tóm tắt phải từ 10 đến 1000 ký tự")]
        public string TomTat { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        [MinLength(20, ErrorMessage = "Nội dung phải có ít nhất 20 ký tự")]
        [Column(TypeName = "nvarchar(max)")]
        public string NoiDung { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập đường dẫn ảnh đại diện")]
        [StringLength(500,
            ErrorMessage = "Đường dẫn ảnh không được vượt quá 500 ký tự")]
        public string AnhDaiDien { get; set; } = string.Empty;

        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn danh mục bài viết")]
        public int MaDanhMuc { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Tác giả không hợp lệ")]
        public int MaNhanVienTacGia { get; set; }

        public int? MaNhanVienDuyet { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [StringLength(20,
            ErrorMessage = "Trạng thái không được vượt quá 20 ký tự")]
        [RegularExpression(
            @"^(Chờ duyệt|Hoàn Thành|Cần Sửa)$",
            ErrorMessage = "Trạng thái bài viết không hợp lệ")]
        public string TrangThai { get; set; } = "Chờ duyệt";

        [Required]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }

        public DateTime? NgayXuatBan { get; set; }
    }
}
