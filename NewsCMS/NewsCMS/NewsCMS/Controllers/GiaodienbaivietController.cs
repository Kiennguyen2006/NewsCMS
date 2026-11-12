using Microsoft.AspNetCore.Mvc;
using NewsCMS.Data;
using NewsCMS.Models;
namespace NewsCMS.Controllers
{
    public class GiaodienbaivietController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public GiaodienbaivietController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public IActionResult ChiTiet(int id, string binhluanthem)
        {
            if (id <= 0)
            {
                return NotFound();
            }
            var dongbaiviet = _context.BaiViet.Find(id);

            if (dongbaiviet == null)
            {
                return NotFound();
            }
            ViewBag.dongbaiviet = dongbaiviet;
            var binhluan = _context.Binhluan.Where(x => x.MaBaiViet == id).ToList();
            ViewBag.binhluan = binhluan;
            if (string.IsNullOrWhiteSpace(binhluanthem))
            {
                return View();
            }
            int? matk = HttpContext.Session.GetInt32("MaTaiKhoan");

            if (matk == null)
            {
                ViewBag.ThongBao = "Bạn phải đăng nhập trước khi bình luận!";
                return View();
            }
            var madocgia = _context.DocGia.FirstOrDefault(x => x.MaTaiKhoan == matk.Value);
            if (madocgia == null)
            {
                ViewBag.ThongBao = "Tài khoản của bạn chưa có thông tin độc giả!";
                return View();
            }
            BinhluanModel binhluanmoi = new BinhluanModel();
            binhluanmoi.MaBinhLuan =(_context.Binhluan.Max(x => (int?)x.MaBinhLuan) ?? 0) + 1;
            binhluanmoi.MaBaiViet = id;
            binhluanmoi.MaDocGia = madocgia.MaDocGia;
            binhluanmoi.NoiDung = binhluanthem;
            binhluanmoi.NgayBinhLuan = DateTime.Now;
            _context.Binhluan.Add(binhluanmoi);
            _context.SaveChanges();
            return RedirectToAction("ChiTiet", new { id = id });
        }
    }
}
