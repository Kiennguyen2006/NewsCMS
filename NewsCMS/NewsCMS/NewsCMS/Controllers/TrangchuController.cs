
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using NewsCMS.Data;
using NewsCMS.Models;
using System;
using System.Threading.Tasks;

namespace NewsCMS.Controllers
{
    public class TrangchuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public TrangchuController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Trangdau(string timkiem, int trang = 1)
        {
            int sodongmuonhienthi = 10;
            ViewBag.sodongmuonhienthi = sodongmuonhienthi;
            var bangchuacacbaivietdadcduyet = _context.BaiViet.Where(x => x.TrangThai == "Hoàn Thành"&& _context.DanhMuc.Any(dm =>dm.MaDanhMuc == x.MaDanhMuc && dm.TrangThai == true)).OrderByDescending(b => b.NgayTao);
            int tongsodong = bangchuacacbaivietdadcduyet.Count();
            ViewBag.soluongbaiviet = tongsodong;
            int tongsotrang = Math.Max(1, (int)Math.Ceiling((double)tongsodong / sodongmuonhienthi));
            trang = Math.Clamp(trang, 1, tongsotrang);
            ViewBag.trang = trang;
            ViewBag.tongsotrang = tongsotrang;
            var bangdanhmuc = _context.DanhMuc.Where(x => x.TrangThai == true).ToList();
            ViewBag.bangdanhmuc = bangdanhmuc;
            var bang = bangchuacacbaivietdadcduyet;
            if (!string.IsNullOrEmpty(timkiem))
            {
                bang = bang.Where(x => x.TieuDe.Contains(timkiem))
                           .OrderByDescending(x => x.NgayTao);
            }
            ViewBag.bang = bang.Skip((trang - 1) * sodongmuonhienthi).Take(sodongmuonhienthi).ToList();
            return View();
        }

        public IActionResult Dangnhap(string TenTaiKhoan, string MatKhau)
        {
            if ((TenTaiKhoan == null || TenTaiKhoan == "") &&
                (MatKhau == "" || MatKhau == null))
            {
                return View();
            }
            else
            {
                int k = 0;
                int mtk = 0;
                int mvt = 0;

                var bangtk = _context.Taikhoan.ToList();

                for (int i = 0; i < bangtk.Count; i++)
                {
                    if (bangtk[i].TenDangNhap.Equals(TenTaiKhoan) &&
                        bangtk[i].MatKhau.Equals(MatKhau))
                    {
                        if (bangtk[i].TrangThai == true)
                        {
                            k = 1;
                            mtk = bangtk[i].MaTaiKhoan;
                            mvt = bangtk[i].MaVaiTro;
                            break;
                        }
                        else
                        {
                            ViewBag.thongbao = "Tài khoản của bạn đã bị khóa vui lòng liên hệ quản trị viên để được hỗ trợ!!!";
                            return View();
                        }
                    }
                    else
                    {
                        k = 0;
                    }
                }

                if (k == 1)
                {
                    HttpContext.Session.SetInt32("MaTaiKhoan", mtk);
                    HttpContext.Session.SetInt32("MaVaiTro", mvt);

                    return RedirectToAction("Trangdau", "Trangchu");
                }
                else
                {
                    ViewBag.thongbao = "Tên tài khoản hoặc mật khẩu không chính xác!!!";
                    return View();
                }
            }

            return View();
        }

        public async Task<IActionResult> Quenmatkhau(string email)
        {
            if (email == "" || email == null)
            {
                return View();
            }
            else
            {
                bool a = false;
                var bangtk = _context.Taikhoan.ToList();

                foreach (var item in bangtk)
                {
                    if (item.Email.Equals(email))
                    {
                        a = true;
                        break;
                    }
                    else
                    {
                        a = false;
                    }
                }

                if (a == true)
                {
                    Random rd = new Random();

                    string ma = rd.Next(100000, 1000000).ToString();

                    TempData["MaOTP"] = ma;
                    TempData["Email"] = email;

                    var message = new MimeMessage();

                    message.From.Add(new MailboxAddress(
                        "Hệ thống quản lý thú cưng",
                        "kiennguyenfa12345678@gmail.com"
                    ));

                    message.To.Add(new MailboxAddress(
                        "",
                        email
                    ));

                    message.Subject = "Mã khôi phục mật khẩu";

                    message.Body = new TextPart("plain")
                    {
                        Text = "Mã khôi phục mật khẩu của bạn là: " + ma +
                               "\n\nMã này dùng để xác nhận khôi phục mật khẩu."
                    };

                    using (var smtp = new SmtpClient())
                    {
                        await smtp.ConnectAsync(
                            "smtp.gmail.com",
                            587,
                            SecureSocketOptions.StartTls
                        );

                        await smtp.AuthenticateAsync(
                            "kiennguyenfa12345678@gmail.com",
                            "cehi hojfejfjzkdu"
                        );

                        await smtp.SendAsync(message);

                        await smtp.DisconnectAsync(true);
                    }

                    return RedirectToAction("Nhapma", "Trangchu");
                }
                else
                {
                    ViewBag.thongbao = "email này không tồn tại trong hệ thống của chúng tôi vui lòng kiểm tra lại thông tin!!!";
                    return View();
                }
            }

            return View();
        }

        public IActionResult Nhapma(string ma)
        {
            if (ma == "" || ma == null)
            {
                return View();
            }
            else
            {
                if (ma == TempData["MaOTP"].ToString())
                {
                    return RedirectToAction("Taolaimk", "Trangchu");
                }
                else
                {
                    ViewBag.thongbao = "Mã nhập không chính xác!";
                    return View();
                }
            }
        }

        public IActionResult Taolaimk(string MatKhauMoi, string XacNhanMatKhau)
        {
            if ((MatKhauMoi == "" || MatKhauMoi == null) &&
                (XacNhanMatKhau == "" || XacNhanMatKhau == null))
            {
                return View();
            }
            else
            {
                bool trangthai = false;

                if (MatKhauMoi == XacNhanMatKhau)
                {
                    var bang = _context.Taikhoan.ToList();

                    for (int i = 0; i < bang.Count; i++)
                    {
                        if (bang[i].Email == TempData["Email"].ToString())
                        {
                            bang[i].MatKhau = MatKhauMoi;
                            trangthai = true;
                            break;
                        }
                        else
                        {
                            trangthai = false;
                        }
                    }

                    if (trangthai == true)
                    {
                        _context.SaveChanges();
                        return RedirectToAction("Dangnhap", "Trangchu");
                    }
                    else
                    {
                        ViewBag.thongbao = "Mật khẩu mới và xác nhận mật khẩu không tùng khớp vui lòng nhập lại";
                        return View();
                    }
                }
            }

            return View();
        }

        public IActionResult Dangky()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Dangkytk(TaiKhoanModel taiKhoan, DocgiaModel docgia)
        {
            bool a = false;

            var bang = _context.Taikhoan.ToList();

            foreach (var i in bang)
            {
                if (i.Email.Equals(taiKhoan.Email)||i.TenDangNhap.Equals(taiKhoan.TenDangNhap))
                {
                    a = false;
                    break;
                }
                else
                {
                    a = true;
                }
            }

            if (a == false)
            {
                ViewBag.thongbao = "Email hoặc tên đăng nhập đã tồn tại vui lòng nhập lại thông tin!!!";
                return View("Dangky");
            }
            else
            {
                int maTaiKhoanMoi = (_context.Taikhoan
                    .Select(x => (int?)x.MaTaiKhoan)
                    .Max() ?? 0) + 1;

                taiKhoan.MaTaiKhoan = maTaiKhoanMoi;
                taiKhoan.TrangThai = true;
                taiKhoan.NgayTao = DateTime.Now;
                taiKhoan.MaVaiTro = 4;

                int maDocGiaMoi = (_context.DocGia
                    .Select(x => (int?)x.MaDocGia)
                    .Max() ?? 0) + 1;

                docgia.MaDocGia = maDocGiaMoi;
                docgia.MaTaiKhoan = maTaiKhoanMoi;
                docgia.NgayDangKy = DateTime.Now;

                _context.Taikhoan.Add(taiKhoan);
                _context.SaveChanges();

                _context.DocGia.Add(docgia);
                _context.SaveChanges();

                return RedirectToAction("Dangnhap", "Trangchu");
            }

            return View();
        }

    }
}
