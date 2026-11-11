using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsCMS.Data;
using NewsCMS.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
namespace NewsCMS.Controllers
{
    public class QuanlybaivietController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public QuanlybaivietController(
            ApplicationDbContext context,
            IConfiguration configuration, 
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
        _env = env;
    }
        public IActionResult Quanlybaiviet(string timkiem)
        {
            if (timkiem == "" || timkiem == null)
            {
                var bang = _context.BaiViet.ToList();
                ViewBag.bang = bang;
                return View();
            }
            else
            {
                var bang = _context.BaiViet.Where(x => x.TieuDe.Contains(timkiem)).ToList();
                ViewBag.bang = bang;
                return View();
            }

        }
        public IActionResult Them()
        {
            var bang = _context.DanhMuc.Where(p=>p.TrangThai== true).ToList();
            ViewBag.bangdm = bang;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Thembv(BaiVietModel baiViet, IFormFile anhFile)
        {
            if (anhFile != null && anhFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(anhFile.FileName);
                string uploadDir = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }
                string filePath = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await anhFile.CopyToAsync(stream);
                }
                baiViet.AnhDaiDien = fileName;
            }
            ModelState.Remove("MaBaiViet");
            ModelState.Remove("MaNhanVienTacGia");
            ModelState.Remove("MaNhanVienDuyet");
            ModelState.Remove("TrangThai");
            ModelState.Remove("NgayTao");
            ModelState.Remove("NgayCapNhat");
            ModelState.Remove("NgayXuatBan");
            ModelState.Remove("AnhDaiDien");
            baiViet.MaBaiViet = (_context.BaiViet.Max(x => (int?)x.MaBaiViet) ?? 0) + 1;
            baiViet.NgayTao = DateTime.Now;
            var tk = HttpContext.Session.GetInt32("MaTaiKhoan");
            var dongchuamanhanvien = _context.NhanVien.FirstOrDefault(x => x.MaTaiKhoan == tk);
            if (dongchuamanhanvien == null)
            {
                return Content("Không tìm thấy nhân viên.");
            }
            baiViet.MaNhanVienTacGia = dongchuamanhanvien.MaNhanVien;
            if (ModelState.IsValid)
            {
                _context.BaiViet.Add(baiViet);
                _context.SaveChanges();
                return RedirectToAction("Quanlybaiviet", "Quanlybaiviet");
            }
            else
            {
                var bang = _context.DanhMuc.Where(p => p.TrangThai == true).ToList();
                return View("Them", baiViet);
            }
        }
        public IActionResult Sua(int id)
        {
            var bang = _context.DanhMuc.Where(p => p.TrangThai == true).ToList();
            ViewBag.bangdm = bang;

            var bangthongtincu = _context.BaiViet.FirstOrDefault(x => x.MaBaiViet == id);
            ViewBag.bangcu = bangthongtincu;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suabv(BaiVietModel baiViet, IFormFile? anhFile, LichSuDuyetModel? lichSuDuyet)
        {
            var vt = HttpContext.Session.GetInt32("MaVaiTro");
            if (vt == 2)
            {
                var baivietcu = _context.BaiViet.FirstOrDefault(x => x.MaBaiViet == baiViet.MaBaiViet);
                if (baivietcu == null)
                {
                    return NotFound();
                }
                if (anhFile != null && anhFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(anhFile.FileName);
                    string uploadDir = Path.Combine(_env.WebRootPath, "images");

                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await anhFile.CopyToAsync(stream);
                    }
                    baiViet.AnhDaiDien = fileName;
                }
                else
                {
                    baiViet.AnhDaiDien = baivietcu.AnhDaiDien;
                }
                ModelState.Remove("MaBaiViet");
                ModelState.Remove("MaNhanVienTacGia");
                ModelState.Remove("MaNhanVienDuyet");
                ModelState.Remove("TrangThai");
                ModelState.Remove("NgayCapNhat");
                ModelState.Remove("NgayXuatBan");
                ModelState.Remove("AnhDaiDien");
                ModelState.Remove("HanhDong");
                baiViet.MaNhanVienTacGia = baivietcu.MaNhanVienTacGia;
                baiViet.MaNhanVienDuyet = baivietcu.MaNhanVienDuyet;
                baiViet.TrangThai = baivietcu.TrangThai;
                baiViet.NgayTao = baivietcu.NgayTao;
                baiViet.NgayXuatBan = baivietcu.NgayXuatBan;
                baiViet.NgayCapNhat = DateTime.Now;
                if (ModelState.IsValid)
                {
                    baivietcu.TieuDe = baiViet.TieuDe;
                    baivietcu.TomTat = baiViet.TomTat;
                    baivietcu.NoiDung = baiViet.NoiDung;
                    baivietcu.AnhDaiDien = baiViet.AnhDaiDien;
                    baivietcu.MaDanhMuc = baiViet.MaDanhMuc;
                    baivietcu.NgayCapNhat = DateTime.Now;
                    _context.SaveChanges();
                    return RedirectToAction("Quanlybaiviet", "Quanlybaiviet");
                }
                else
                {
                    var bang = _context.DanhMuc.Where(p => p.TrangThai == true).ToList();
                    ViewBag.bangdm = bang;  
                    ViewBag.bangcu = baiViet;
                }
            }
            else if (vt == 3)
            {
                var baivietcu = _context.BaiViet.FirstOrDefault(x => x.MaBaiViet == baiViet.MaBaiViet);
                if (baivietcu == null)
                {
                    return NotFound();
                }
                if (anhFile != null && anhFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(anhFile.FileName);
                    string uploadDir = Path.Combine(_env.WebRootPath, "images");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await anhFile.CopyToAsync(stream);
                    }
                    baiViet.AnhDaiDien = fileName;
                }
                else
                {
                    baiViet.AnhDaiDien = baivietcu.AnhDaiDien;
                }
                ModelState.Remove("MaBaiViet");
                ModelState.Remove("MaNhanVienTacGia");
                ModelState.Remove("MaNhanVienDuyet");
                ModelState.Remove("NgayCapNhat");
                ModelState.Remove("NgayXuatBan");
                ModelState.Remove("AnhDaiDien");
                ModelState.Remove("HanhDong");
                var tk = HttpContext.Session.GetInt32("MaTaiKhoan");
                var dongchuamanhanvien = _context.NhanVien.FirstOrDefault(x => x.MaTaiKhoan == tk);
                baiViet.NgayTao = baivietcu.NgayTao;
                baiViet.MaNhanVienTacGia = baivietcu.MaNhanVienTacGia;
                baiViet.NgayCapNhat = DateTime.Now;
                baiViet.NgayXuatBan = DateTime.Now;
                baiViet.MaNhanVienDuyet = dongchuamanhanvien.MaNhanVien;
                if (ModelState.IsValid)
                {
                    baivietcu.TieuDe = baiViet.TieuDe;
                    baivietcu.TomTat = baiViet.TomTat;
                    baivietcu.NoiDung = baiViet.NoiDung;
                    baivietcu.AnhDaiDien = baiViet.AnhDaiDien;
                    baivietcu.MaDanhMuc = baiViet.MaDanhMuc;
                    baivietcu.TrangThai = baiViet.TrangThai;
                    baivietcu.NgayCapNhat = DateTime.Now;
                    baivietcu.NgayXuatBan = DateTime.Now;
                    baivietcu.MaNhanVienDuyet = dongchuamanhanvien.MaNhanVien;
                    _context.SaveChanges();
                    LichSuDuyetModel lichSu = new LichSuDuyetModel();
                    int maLichSu = 1;

                    if (_context.LichSuDuyet.Any())
                    {
                        maLichSu = _context.LichSuDuyet.Max(x => x.MaLichSu) + 1;
                    }
                    lichSu.MaLichSu = maLichSu;
                    lichSu.MaBaiViet = baiViet.MaBaiViet;
                    lichSu.MaNguoiDuyet = dongchuamanhanvien.MaNhanVien;
                    lichSu.HanhDong = baiViet.TrangThai;
                    lichSu.ThoiGian = DateTime.Now;
                    lichSu.GhiChu = "Không có";
                    _context.LichSuDuyet.Add(lichSu);
                    _context.SaveChanges();
                    return RedirectToAction("Quanlybaiviet", "Quanlybaiviet");
                }
                else
                {
                    var bang = _context.DanhMuc.Where(p => p.TrangThai == true).ToList();
                    ViewBag.bangdm = bang; 
                    ViewBag.bangcu = baiViet;
                }
            }
            return RedirectToAction("Quanlybaiviet", "Quanlybaiviet");
        }
        public IActionResult Xoa(int id)
        {
            var dongthongtincu = _context.BaiViet.FirstOrDefault(x => x.MaBaiViet == id);
            if (dongthongtincu == null)
            {
                return NotFound();
            }
            ViewBag.dongcu = dongthongtincu;
            return View();
        }
        [HttpPost]

        public IActionResult Xoabv(BaiVietModel baiViet, int id, LichSuDuyetModel lichSuDuyet)
        {
            var dongchuabv = _context.BaiViet.FirstOrDefault(x => x.MaBaiViet==id);
            if (dongchuabv==null)
            {
                return NotFound();
            }
            if (dongchuabv.MaNhanVienDuyet==null)
            {
                ViewBag.dongcu = dongchuabv;
                ViewBag.thongbao = "Bài viết phải được chru biên tập duyệt trước khi xoá!!!";
                return View("thongbao");
            }
            else
            {
                var donglsxoa = _context.LichSuDuyet.Where(x => x.MaBaiViet == id).ToList();
                if (donglsxoa.Count <= 0)
                {
                    return NotFound();
                }
                else
                {
                    _context.LichSuDuyet.RemoveRange(donglsxoa);
                    _context.SaveChanges();
                }
                var dongblxoa = _context.Binhluan.Where(x => x.MaBaiViet == baiViet.MaBaiViet).ToList();
                if (dongblxoa.Count <= 0)
                {
                    return NotFound();
                }
                else
                {
                    _context.Binhluan.RemoveRange(dongblxoa);
                    _context.SaveChanges();
                }
                var dongxoa = _context.BaiViet.FirstOrDefault(x => x.MaBaiViet == id);
                if (dongxoa == null)
                {
                    return View("Xoa", baiViet);
                }
                else
                {
                    _context.BaiViet.Remove(dongxoa);
                    _context.SaveChanges();
                    return RedirectToAction("Quanlybaiviet", "Quanlybaiviet");
                }
            }
        }
    }
}
