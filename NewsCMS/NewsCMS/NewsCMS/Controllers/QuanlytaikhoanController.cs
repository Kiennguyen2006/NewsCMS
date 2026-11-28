using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewsCMS.Data;
using NewsCMS.Models;

namespace NewsCMS.Controllers
{
    public class QuanlytaikhoanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public QuanlytaikhoanController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }
        public IActionResult Quanlytaikhoan(string timkiem)
        {
            if (timkiem != null && timkiem != "")
            {
                var bang = _context.Taikhoan.Where(x => x.TenDangNhap.Contains(timkiem) || x.Email.Contains(timkiem)).ToList();
                ViewBag.bang = bang;
            }
            else
            {
                var bang = _context.Taikhoan.ToList();
                ViewBag.bang = bang;
            }
            return View();
        }
        public IActionResult Suataikhoan(int id)
        {
            var dongthongtincu = _context.Taikhoan.FirstOrDefault(x => x.MaTaiKhoan.Equals(id));
            ViewBag.dongthongtincu = dongthongtincu;
            ViewBag.vaitro = new SelectList(_context.Vaitro.ToList(), "MaVaiTro", "TenVaiTro", dongthongtincu.MaVaiTro);
            return View(dongthongtincu);
        }
        [HttpPost]
        public IActionResult Suatk(TaiKhoanModel taiKhoan)
        {
            if (ModelState.IsValid)
            {
                var taiKhoanCu = _context.Taikhoan.AsNoTracking().FirstOrDefault(x => x.MaTaiKhoan == taiKhoan.MaTaiKhoan);

                if (taiKhoanCu != null && taiKhoan.NgayTao == default)
                {
                    taiKhoan.NgayTao = taiKhoanCu.NgayTao;
                }
                bool tt = true;
                var bang = _context.Taikhoan.Where(x => x.MaTaiKhoan != taiKhoan.MaTaiKhoan).ToList();
                foreach (var i in bang)
                {
                    if (taiKhoan.Email.Equals(i.Email))
                    {
                        tt = false;
                        break;
                    }
                }
                if (tt == true)
                {
                    _context.Taikhoan.Update(taiKhoan);
                    _context.SaveChanges();
                    return RedirectToAction("Quanlytaikhoan", "Quanlytaikhoan");
                }
                else
                {
                    ViewBag.vaitro = new SelectList(_context.Vaitro.ToList(), "MaVaiTro", "TenVaiTro", taiKhoan.MaVaiTro);
                    ViewBag.thongbao = "Email đã tồn tại";
                    return View("Suataikhoan", taiKhoan);
                }
            }
            else
            {
                ViewBag.vaitro = new SelectList(_context.Vaitro.ToList(), "MaVaiTro", "TenVaiTro", taiKhoan.MaVaiTro);
                ViewBag.thongbao = "Có lỗi xảy ra khi cập nhật tài khoản!!!";
                return View("Suataikhoan", taiKhoan);
            }
        }
        public IActionResult Xoa(int id)
        {
            var bang = _context.Taikhoan.FirstOrDefault(x => x.MaTaiKhoan == id);
            ViewBag.bang = bang;
            return View();
        }
        [HttpPost]
        public IActionResult Xoatk(TaiKhoanModel taiKhoan)
        {
            int matk = taiKhoan.MaTaiKhoan;
            int mavt = taiKhoan.MaVaiTro;
            var dongnv = _context.NhanVien.FirstOrDefault(x => x.MaTaiKhoan == matk);
            if (dongnv != null)
            {
                int manv = dongnv.MaNhanVien;
                if (mavt == 2)
                {
                    var dongdg = _context.DocGia.FirstOrDefault(x => x.MaTaiKhoan == matk);
                    if (dongdg != null)
                    {
                        int mabl = dongdg.MaDocGia;
                        var blDocGia = _context.Binhluan.Where(x => x.MaDocGia == mabl).ToList();
                        if (blDocGia.Count > 0)
                        {
                            _context.Binhluan.RemoveRange(blDocGia);
                            _context.SaveChanges();
                        }
                        _context.DocGia.Remove(dongdg);
                        _context.SaveChanges();
                    }
                    var bangbvcanxoa = _context.BaiViet.Where(x => x.MaNhanVienTacGia == manv).ToList();
                    if (bangbvcanxoa.Count > 0)
                    {
                        var danhsachmabv = bangbvcanxoa.Select(x => x.MaBaiViet).ToList();
                        var cacdongbinhluancanxoa = _context.Binhluan.Where(x => danhsachmabv.Contains(x.MaBaiViet)).ToList();
                        if (cacdongbinhluancanxoa.Count > 0)
                        {
                            _context.Binhluan.RemoveRange(cacdongbinhluancanxoa);
                            _context.SaveChanges();
                        }
                        var banglscanxoa = _context.LichSuDuyet.Where(x => danhsachmabv.Contains(x.MaBaiViet)).ToList();
                        if (banglscanxoa.Count > 0)
                        {
                            _context.LichSuDuyet.RemoveRange(banglscanxoa);
                            _context.SaveChanges();
                        }
                        _context.BaiViet.RemoveRange(bangbvcanxoa);
                        _context.SaveChanges();
                    }
                    _context.NhanVien.Remove(dongnv);
                    _context.SaveChanges();
                    var bangtk = _context.Taikhoan.FirstOrDefault(x => x.MaTaiKhoan == matk);
                    if (bangtk != null)
                    {
                        _context.Taikhoan.Remove(bangtk);
                        _context.SaveChanges();
                    }
                    return RedirectToAction("Quanlytaikhoan", "Quanlytaikhoan");
                }
                else if (mavt == 3)
                {
                    var dongdg = _context.DocGia.FirstOrDefault(x => x.MaTaiKhoan == matk);
                    if (dongdg != null)
                    {
                        int mabl = dongdg.MaDocGia;
                        var blDocGia = _context.Binhluan.Where(x => x.MaDocGia == mabl).ToList();
                        if (blDocGia.Count > 0)
                        {
                            _context.Binhluan.RemoveRange(blDocGia);
                            _context.SaveChanges();
                        }
                        _context.DocGia.Remove(dongdg);
                        _context.SaveChanges();
                    }
                    var bangbvcanxoa = _context.BaiViet.Where(x => x.MaNhanVienDuyet == manv).ToList();
                    var danhsachmabv = bangbvcanxoa.Select(x => x.MaBaiViet).ToList();
                    if (danhsachmabv.Count > 0)
                    {
                        var cacdongbinhluancanxoa = _context.Binhluan.Where(x => danhsachmabv.Contains(x.MaBaiViet)).ToList();
                        if (cacdongbinhluancanxoa.Count > 0)
                        {
                            _context.Binhluan.RemoveRange(cacdongbinhluancanxoa);
                            _context.SaveChanges();
                        }
                    }
                    var banglscanxoa = _context.LichSuDuyet.Where(x => danhsachmabv.Contains(x.MaBaiViet) || x.MaNguoiDuyet == manv).ToList();
                    if (banglscanxoa.Count > 0)
                    {
                        _context.LichSuDuyet.RemoveRange(banglscanxoa);
                        _context.SaveChanges();
                    }
                    if (bangbvcanxoa.Count > 0)
                    {
                        _context.BaiViet.RemoveRange(bangbvcanxoa);
                        _context.SaveChanges();
                    }
                    _context.NhanVien.Remove(dongnv);
                    _context.SaveChanges();
                    var bangtk = _context.Taikhoan.FirstOrDefault(x => x.MaTaiKhoan == matk);
                    if (bangtk != null)
                    {
                        _context.Taikhoan.Remove(bangtk);
                        _context.SaveChanges();
                    }
                    return RedirectToAction("Quanlytaikhoan", "Quanlytaikhoan");
                }
                else if (mavt == 1)
                {
                    var dongdg = _context.DocGia.FirstOrDefault(x => x.MaTaiKhoan == matk);
                    if (dongdg != null)
                    {
                        int madg = dongdg.MaDocGia;
                        var blDocGia = _context.Binhluan.Where(x => x.MaDocGia == madg).ToList();
                        if (blDocGia.Count > 0)
                        {
                            _context.Binhluan.RemoveRange(blDocGia);
                            _context.SaveChanges();
                        }
                        _context.DocGia.Remove(dongdg);
                        _context.SaveChanges();
                    }
                    _context.NhanVien.Remove(dongnv);
                    _context.SaveChanges();
                    var bangtk = _context.Taikhoan.FirstOrDefault(x => x.MaTaiKhoan == matk);
                    if (bangtk != null)
                    {
                        _context.Taikhoan.Remove(bangtk);
                        _context.SaveChanges();
                    }
                    var tkDangNhap = HttpContext.Session.GetInt32("MaTaiKhoan");
                    if (tkDangNhap != null && tkDangNhap == matk)
                    {
                        HttpContext.Session.Clear();
                        return RedirectToAction("Trangdau", "Trangchu");
                    }
                    return RedirectToAction("Quanlytaikhoan", "Quanlytaikhoan");
                }
            }
            else
            {
                var dongdg = _context.DocGia.FirstOrDefault(x => x.MaTaiKhoan == matk);
                if (dongdg != null)
                {
                    int madg = dongdg.MaDocGia;
                    var cacdongbinhluancanxoa = _context.Binhluan.Where(x => x.MaDocGia == madg).ToList();
                    if (cacdongbinhluancanxoa.Count > 0)
                    {
                        _context.Binhluan.RemoveRange(cacdongbinhluancanxoa);
                        _context.SaveChanges();
                    }
                    _context.DocGia.Remove(dongdg);
                    _context.SaveChanges();
                }
                var bangtk1 = _context.Taikhoan.Find(matk);
                if (bangtk1 != null)
                {
                    _context.Taikhoan.Remove(bangtk1);
                    _context.SaveChanges();
                }
                return RedirectToAction("Quanlytaikhoan", "Quanlytaikhoan");
            }
            return RedirectToAction("Quanlytaikhoan", "Quanlytaikhoan");
        }
    }
}
