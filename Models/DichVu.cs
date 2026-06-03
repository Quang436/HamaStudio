using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        public int MaDichVu { get; set; }

        public int? MaDanhMuc { get; set; }

        [Required]
        [StringLength(200)]
        public string TenDichVu { get; set; }

        [Column(TypeName = "decimal")]
        public decimal GiaTien { get; set; } = 0;

        public int GioiHanTho { get; set; } = 1;

        public string MoTa { get; set; }

        [StringLength(255)]
        public string LinkAnhDaiDien { get; set; }

        [ForeignKey("MaDanhMuc")]
        public virtual DanhMucDichVu DanhMucDichVu { get; set; }

        public virtual ICollection<DatLich> DatLiches { get; set; }
        public virtual ICollection<Portfolio> Portfolios { get; set; }
        public virtual ICollection<DacQuyen> DacQuyens { get; set; }
        public virtual ICollection<DichVuBoSung> DichVuBoSungs { get; set; }
        public virtual ICollection<LichChup> LichChups { get; set; }
    }
}
