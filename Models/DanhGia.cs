using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("DanhGia")]
    public class DanhGia
    {
        [Key]
        public int MaDanhGia { get; set; }

        public int? MaDatLich { get; set; }

        public int? MaKhachHang { get; set; }

        [Range(1, 5)]
        public int? SoSao { get; set; }

        public string NoiDungBinhLuan { get; set; }

        public DateTime? NgayGui { get; set; } = DateTime.Now;

        [ForeignKey("MaDatLich")]
        public virtual DatLich DatLich { get; set; }

        [ForeignKey("MaKhachHang")]
        public virtual KhachHang KhachHang { get; set; }
    }
}
