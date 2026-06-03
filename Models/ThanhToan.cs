using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("ThanhToan")]
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        public int? MaDatLich { get; set; }

        [Column(TypeName = "decimal")]
        public decimal SoTienThanhToan { get; set; }

        [StringLength(50)]
        public string PhuongThuc { get; set; }

        public DateTime? NgayGiaoDich { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string NoiDungThanhToan { get; set; }

        [ForeignKey("MaDatLich")]
        public virtual DatLich DatLich { get; set; }
    }
}
