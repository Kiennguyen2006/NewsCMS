using Microsoft.AspNetCore.Mvc;
using NewsCMS.Data;

namespace NewsCMS.Controllers
{
    public class HosoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public HosoController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult Hoso()
        {
            int? matk = HttpContext.Session.GetInt32("MaTaiKhoan");
            var dongtk = _context.Taikhoan.Find(matk);
            var dongdg = _context.DocGia.FirstOrDefault(x => x.MaTaiKhoan == matk.Value);
            ViewBag.dongtk = dongtk;
            ViewBag.dongdg = dongdg;
            return PartialView("Hoso");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Dangxuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Trangdau","Trangchu");
        }
    }
}
