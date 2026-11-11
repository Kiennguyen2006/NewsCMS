using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NewsCMS.Models;

namespace NewsCMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BaiVietModel> BaiViet { get; set; }
        public DbSet<DanhMucModel> DanhMuc { get; set; }
        public DbSet<TaiKhoanModel> Taikhoan { get; set; }
        public DbSet<DocgiaModel> DocGia { get; set; }
        public DbSet<NhanVienModel> NhanVien { get; set; }
        public DbSet<LichSuDuyetModel> LichSuDuyet { get; set; }
        public DbSet<VaitroModel> Vaitro { get; set; }
        public DbSet<BinhluanModel> Binhluan { get; set; }
    }
}