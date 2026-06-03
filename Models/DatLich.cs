using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("DatLich")]
    public class DatLich
    {
        [Key]
        public int MaDatLich { get; set; }

        public int? MaKhachHang { get; set; }

        public int? MaDichVu { get; set; }

        public int? MaLichChup { get; set; }

        public DateTime? NgayHeThongGhiNhan { get; set; } = DateTime.Now;

        [Column(TypeName = "date")]
        public DateTime NgayChup { get; set; }

        [Required]
        [StringLength(20)]
        public string KhungGio { get; set; }

        [StringLength(255)]
        public string DiaDiem { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? TongTien { get; set; } = 0;

        [Column(TypeName = "decimal")]
        public decimal? SoTienDaCoc { get; set; } = 0;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Chờ xác nhận";

        public string GhiChu { get; set; }

        [StringLength(500)]
        public string LinkAnhBanGiao { get; set; }

        [ForeignKey("MaKhachHang")]
        public virtual KhachHang KhachHang { get; set; }

        [ForeignKey("MaDichVu")]
        public virtual DichVu DichVu { get; set; }

        [ForeignKey("MaLichChup")]
        public virtual LichChup LichChup { get; set; }

        public virtual ICollection<ThanhToan> ThanhToans { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
    }
}
