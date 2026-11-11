using Microsoft.AspNetCore.Mvc;
using NewsCMS.Data;
using NewsCMS.Models;

namespace NewsCMS.Controllers
{
    public class DanhmucController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public DanhmucController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }
        public IActionResult Danhmuc(string? ma)
        {
            if (ma == null)
            {
                var bang = _context.DanhMuc.ToList();
                ViewBag.bang = bang;
                return View();
            }
            else
            {
                var bang=_context.DanhMuc.Where(o=>o.TenDanhMuc==ma).ToList();
                ViewBag.bang = bang;
                return View();
            }
   
        }

        public IActionResult Themdanhmuc()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Themdm(DanhMucModel danhMuc)
        {
            ModelState.Remove("MaDanhMuc");
            if (ModelState.IsValid)
            {
                danhMuc.MaDanhMuc = (_context.DanhMuc.Max(x => (int?)x.MaDanhMuc) ?? 0) + 1;
                _context.DanhMuc.Add(danhMuc);
                _context.SaveChanges();
                return RedirectToAction("Danhmuc", "Danhmuc");
            }
            return View();
        }
        public IActionResult Suadanhmuc(int? ma)
        {
            if (ma == null)
            {
                return NotFound();
            }
            else
            {
                var bang = _context.DanhMuc.FirstOrDefault(x => x.MaDanhMuc == ma);
                ViewBag.bang = bang;
                return View();
            }
            return View();
        }
        [HttpPost]
        public IActionResult Suadm(DanhMucModel danhMuc)
        {
            if (ModelState.IsValid) {
                _context.DanhMuc.Update(danhMuc);
                _context.SaveChanges();
                return RedirectToAction("Danhmuc", "Danhmuc");
            }
            return View();
        }
    }
}