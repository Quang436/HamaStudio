using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("LichChup")]
    public class LichChup
    {
        [Key]
        public int MaLichChup { get; set; }

        public int? MaDichVu { get; set; }

        [Column(TypeName = "date")]
        [Required]
        public DateTime NgayChup { get; set; }

        [Required]
        public int SoLichToiDa { get; set; }

        [Required]
        public int SoLichConLai { get; set; }

        public string GhiChu { get; set; }

        public DateTime? NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "Hoạt động"; // Hoạt động, Đã hủy, Đã đầy

        [ForeignKey("MaDichVu")]
        public virtual DichVu DichVu { get; set; }

        public virtual ICollection<DatLich> DatLiches { get; set; }
    }
}
