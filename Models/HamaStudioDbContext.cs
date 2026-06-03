using System.Data.Entity;

namespace HamaStudio.Models
{
    public class HamaStudioDbContext : DbContext
    {
        public HamaStudioDbContext() : base("name=DefaultConnection")
        {
            // Tự động kiểm tra và thêm cột LinkAnhBanGiao nếu chưa có trong DB
            Database.ExecuteSqlCommand("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DatLich') AND name = 'LinkAnhBanGiao') ALTER TABLE DatLich ADD LinkAnhBanGiao NVARCHAR(500) NULL;");
            // Cho phép SoDienThoai NULL (vì form đăng ký không yêu cầu SĐT nữa)
            Database.ExecuteSqlCommand("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhachHang') AND name = 'SoDienThoai' AND is_nullable = 0) ALTER TABLE KhachHang ALTER COLUMN SoDienThoai VARCHAR(15) NULL;");
        }

        public virtual DbSet<QuanTriVien> QuanTriViens { get; set; }
        public virtual DbSet<KhachHang> KhachHangs { get; set; }
        public virtual DbSet<DanhMucDichVu> DanhMucDichVus { get; set; }
        public virtual DbSet<DichVu> DichVus { get; set; }
        public virtual DbSet<DatLich> DatLiches { get; set; }
        public virtual DbSet<ThanhToan> ThanhToans { get; set; }
        public virtual DbSet<DanhGia> DanhGias { get; set; }
        public virtual DbSet<Portfolio> Portfolios { get; set; }
        public virtual DbSet<DacQuyen> DacQuyens { get; set; }
        public virtual DbSet<DichVuBoSung> DichVuBoSungs { get; set; }
        public virtual DbSet<LichChup> LichChups { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Cấu hình các quan hệ nếu cần thiết (Fluent API)
            base.OnModelCreating(modelBuilder);
        }
    }
}
