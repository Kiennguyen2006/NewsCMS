using Microsoft.AspNetCore.Mvc;
using NewsCMS.Data;

namespace NewsCMS.Controllers
{
    public class LichsuduyetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public LichsuduyetController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }
        public IActionResult Lichsuduyet(int? matk)
        {
            if (matk == null)
            {
                var bang = _context.LichSuDuyet.ToList();
                ViewBag.bang = bang;
                return View();
            }
            else
            {
                var bang = _context.LichSuDuyet.Where(p=>p.MaLichSu==matk || p.MaBaiViet==matk || p.MaNguoiDuyet==matk).ToList();
                return View();
            }
        }
    }
}
