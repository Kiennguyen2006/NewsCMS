using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsCMS.Data;
using NewsCMS.Models;

namespace NewsCMS.Controllers
{
    public class NhanvienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public NhanvienController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Nhanvien(string tk)
        {
            if (tk == null || tk == "")
            {
                var bang = _context.NhanVien.ToList();
                ViewBag.bang = bang;
                return View();
            }
            else
            {
                var bang = _context.NhanVien.Where(x => x.HoTen.Contains(tk) || x.SoDienThoai.Contains(tk)).ToList();
                ViewBag.bang = bang;
                return View();
            }
        }
        public IActionResult Themnhanvien()
        {
            var bangtknvdadung = _context.NhanVien.Select(x => x.MaTaiKhoan);
            var bangtk = _context.Taikhoan.Where(x => x.TrangThai == true && !bangtknvdadung.Contains(x.MaTaiKhoan)).ToList();
            ViewBag.bangtk = bangtk;
            return View();
        }
        [HttpPost]
        public IActionResult Themnv(NhanVienModel nhanVien)
        {
            ModelState.Remove("MaNhanVien");
            bool tt = true;
            if (ModelState.IsValid)
            {
                var bang = _context.NhanVien.ToList();
                for (int i = 0; i < bang.Count; i++)
                {
                    if (bang[i].SoDienThoai == nhanVien.SoDienThoai)
                    {
                        tt = false;
                        break;
                    }
                    else
                    {
                        tt = true;
                    }
                }
                if (tt == true)
                {
                    nhanVien.MaNhanVien = (_context.NhanVien.Max(x => (int?)x.MaNhanVien) ?? 0) + 1;
                    _context.NhanVien.Add(nhanVien);
                    _context.SaveChanges();
                    return RedirectToAction("Nhanvien", "Nhanvien");
                }
                else
                {
                    var bangtk = _context.Taikhoan.Where(x => x.TrangThai == true).ToList();
                    ViewBag.bangtk = bangtk;
                    ViewBag.thongbao = "Số điện thoại đã tồn tại vui lòng nhập số khác!!!";
                    return View("Themnhanvien", nhanVien);
                }
            }
            else
            {
                var bangtk = _context.Taikhoan.Where(x => x.TrangThai == true).ToList();
                ViewBag.bangtk = bangtk;
                ViewBag.thongbao = "Dữ liệu nhập vào ko hợp lệ vui lòng nhập lại!!!";
                return View("Themnhanvien", nhanVien);
            }
        }
        public IActionResult Suanhanvien(int? ma)
        {
            if (ma == null)
            {
                return NotFound();
            }
            else
            {
                var bangtk = _context.Taikhoan.Where(tk => tk.TrangThai == true && !_context.NhanVien.Any(nv => nv.MaTaiKhoan == tk.MaTaiKhoan)).ToList();
                ViewBag.bangtk = bangtk;
                var bangthongtincu = _context.NhanVien.FirstOrDefault(x => x.MaNhanVien == ma);
                ViewBag.bangthongtincu = bangthongtincu;
                return View();
            }
        }
        [HttpPost]
        public IActionResult Suanv(NhanVienModel nhanVien)
        {
            if (ModelState.IsValid)
            {
                _context.NhanVien.Update(nhanVien);
                _context.SaveChanges();
                return RedirectToAction("Nhanvien", "Nhanvien");
            }
            else
            {
                var bangtk = _context.Taikhoan.Where(tk => tk.TrangThai == true && !_context.NhanVien.Any(nv => nv.MaTaiKhoan == tk.MaTaiKhoan)).ToList();
                ViewBag.bangtk = bangtk;
                var bangthongtincu = _context.NhanVien.FirstOrDefault(x => x.MaNhanVien == nhanVien.MaNhanVien);
                ViewBag.thongbao = "Dữ liệu nhập vào ko hợp lệ vui lòng nhập lại!!!";
                return View("Suanhanvien", nhanVien);
            }
        }
        public IActionResult Xoanhanvien(int? ma)
        {
            if (ma == null)
            {
                return NotFound();
            }
            else
            {
                var bang = _context.NhanVien.FirstOrDefault(x => x.MaNhanVien == ma);
                ViewBag.bang = bang;
                return View();
            }
        }
        [HttpPost]
        public IActionResult Xoanv(NhanVienModel nhanVien)
        {
            if (ModelState.IsValid)
            {
                var dongtk = _context.Taikhoan.FirstOrDefault(x => x.MaTaiKhoan == nhanVien.MaTaiKhoan);
                var mavt = dongtk.MaVaiTro;
                if (mavt == 2)
                {
                    var dongxoa = _context.NhanVien.Find(nhanVien.MaNhanVien);
                    if (dongxoa == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        var bangbv = _context.BaiViet.Where(x => x.MaNhanVienTacGia == nhanVien.MaNhanVien).ToList();
                        var cacbinhluancuabaoviet = _context.Binhluan.Where(x => bangbv.Select(p => p.MaBaiViet).ToList().Contains(x.MaBaiViet)).ToList();
                        var banglichsuduyet = _context.LichSuDuyet.Where(x => bangbv.Select(y => y.MaBaiViet).ToList().Contains(x.MaBaiViet)).ToList();
                        _context.Binhluan.RemoveRange(cacbinhluancuabaoviet);
                        _context.LichSuDuyet.RemoveRange(banglichsuduyet);
                        _context.BaiViet.RemoveRange(bangbv);
                        _context.NhanVien.Remove(dongxoa);
                        _context.SaveChanges();
                    }
                }
                else if (mavt == 3)
                {
                    var dongxoa = _context.NhanVien.Find(nhanVien.MaNhanVien);
                    if (dongxoa == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        var bangbv = _context.BaiViet.Where(x => x.MaNhanVienDuyet == nhanVien.MaNhanVien).ToList();
                        var cacbinhluancuabaoviet = _context.Binhluan.Where(x => bangbv.Select(p => p.MaBaiViet).ToList().Contains(x.MaBaiViet)).ToList();
                        var banglichsuduyet = _context.LichSuDuyet.Where(x => bangbv.Select(y => y.MaBaiViet).ToList().Contains(x.MaBaiViet)).ToList();
                        _context.Binhluan.RemoveRange(cacbinhluancuabaoviet);
                        _context.LichSuDuyet.RemoveRange(banglichsuduyet);
                        _context.BaiViet.RemoveRange(bangbv);
                        _context.NhanVien.Remove(dongxoa);
                        _context.SaveChanges();
                    }
                }
                else if (mavt == 1 || mavt == 4)
                {
                    var dong = _context.NhanVien.Find(nhanVien.MaNhanVien);
                    _context.NhanVien.Remove(dong);
                    _context.SaveChanges();
                }
            }
            return RedirectToAction("Nhanvien", "Nhanvien");
        }
    }
}
