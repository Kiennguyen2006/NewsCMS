namespace NewsCMS.Models
{
    public class ThongKeViewModel
    {
        public int TongBaiViet { get; set; }
        public int HoanThanh { get; set; }
        public int ChoDuyet { get; set; }
        public int CanSua { get; set; }
        public List<string> NhanThang { get; set; } = new();
        public List<int> BaiVietTheoThang { get; set; } = new();
        public List<string> TenTrangThai { get; set; } = new();
        public List<int> SoLuongTrangThai { get; set; } = new();
    }
}