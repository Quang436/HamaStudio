using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("QuanTriVien")]
    public class QuanTriVien
    {
        [Key]
        public int MaAdmin { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(15)]
        public string SoDienThoai { get; set; }

        [StringLength(50)]
        public string CapBac { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayVaoLam { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "Hoạt động";
    }
}
