using Microsoft.AspNetCore.Mvc;
using NewsCMS.Data;
using NewsCMS.Models;

namespace NewsCMS.Controllers
{
    public class ThongkeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThongkeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Thongke()
        {
            var danhSachBaiViet = _context.BaiViet.ToList();
            var model = new ThongKeViewModel
            {
                TongBaiViet = danhSachBaiViet.Count,

                HoanThanh = danhSachBaiViet.Count(
                    x => x.TrangThai == "Hoàn Thành"),

                ChoDuyet = danhSachBaiViet.Count(
                    x => x.TrangThai == "Chờ duyệt"),

                CanSua = danhSachBaiViet.Count(
                    x => x.TrangThai == "Cần Sửa"),

                TenTrangThai = new List<string>
                {
                    "Hoàn Thành",
                    "Chờ duyệt",
                    "Cần Sửa"
                },

                SoLuongTrangThai = new List<int>
                {
                    danhSachBaiViet.Count(x => x.TrangThai == "Hoàn Thành"),
                    danhSachBaiViet.Count(x => x.TrangThai == "Chờ duyệt"),
                    danhSachBaiViet.Count(x => x.TrangThai == "Cần Sửa")
                }
            };
            DateTime homNay = DateTime.Today;

            DateTime thangDau = new DateTime(
                homNay.Year,
                homNay.Month,
                1
            ).AddMonths(-5);

            for (int i = 0; i < 6; i++)
            {
                DateTime thang = thangDau.AddMonths(i);
                DateTime thangTiepTheo = thang.AddMonths(1);

                model.NhanThang.Add(thang.ToString("MM/yyyy"));

                int soLuong = danhSachBaiViet.Count(x =>
                    x.NgayTao >= thang &&
                    x.NgayTao < thangTiepTheo
                );

                model.BaiVietTheoThang.Add(soLuong);
            }
            return View(model);
        }
    }
}