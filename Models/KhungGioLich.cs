using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("KhungGioLich")]
    public class KhungGioLich
    {
        [Key]
        public int MaKhungGio { get; set; }

        [Required]
        public int MaLichChup { get; set; }

        /// <summary>Giờ bắt đầu, VD: "08:00"</summary>
        [Required]
        [StringLength(10)]
        public string GioBatDau { get; set; }

        /// <summary>Giờ kết thúc, VD: "10:00"</summary>
        [Required]
        [StringLength(10)]
        public string GioKetThuc { get; set; }

        /// <summary>Trạng thái: "Hoạt động" | "Đã khóa"</summary>
        [StringLength(20)]
        public string TrangThai { get; set; } = "Hoạt động";

        /// <summary>Ghi chú lý do khoá (nếu có)</summary>
        public string GhiChu { get; set; }

        public DateTime? NgayTao { get; set; } = DateTime.Now;

        [ForeignKey("MaLichChup")]
        public virtual LichChup LichChup { get; set; }
    }
}
