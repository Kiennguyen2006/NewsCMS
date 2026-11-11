
using System.ComponentModel.DataAnnotations;
namespace NewsCMS.Models
{
    public class DanhMucModel
    {
        [Key]
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        public string TenDanhMuc { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public bool TrangThai { get; set; } = true;
    }
}
