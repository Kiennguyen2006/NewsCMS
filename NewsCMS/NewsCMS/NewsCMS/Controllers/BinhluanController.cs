using Microsoft.AspNetCore.Mvc;
using NewsCMS.Data;

namespace NewsCMS.Controllers
{
    public class BinhluanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public BinhluanController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Binhluan(string tk)
        {
            if(tk==null || tk == "")
            {
                var bang = _context.Binhluan.ToList();
                ViewBag.bang = bang;
            }
            else
            {
                var bang=_context.Binhluan.Where(x=>x.NoiDung.Contains(tk)).ToList();
                ViewBag.bang = bang;
            }
            return View();
        }
        [HttpGet]
        public IActionResult Xacnhanxoa(int? ma)
        {
            if (ma == null)
            {
                return NotFound();
            }
            var dongxoa = _context.Binhluan.FirstOrDefault(x => x.MaBinhLuan == ma);
            if (dongxoa == null)
            {
                return NotFound();
            }
            ViewBag.dongxoa = dongxoa;
           return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int ma)
        {
            var dongxoa = _context.Binhluan.Find(ma);
            if (dongxoa == null)
            {
                return NotFound();
            }
            _context.Binhluan.Remove(dongxoa);
            _context.SaveChanges();
            return RedirectToAction("Binhluan", "Binhluan");
        }
    }
}
