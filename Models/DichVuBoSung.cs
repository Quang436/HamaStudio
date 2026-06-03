using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("DichVuBoSung")]
    public class DichVuBoSung
    {
        [Key]
        public int MaDVBS { get; set; }

        public int? MaDichVu { get; set; }

        [Required]
        [StringLength(200)]
        public string TenDVBS { get; set; }

        [StringLength(500)]
        public string MoTa { get; set; }

        [Column(TypeName = "decimal")]
        public decimal GiaTien { get; set; } = 0;

        [ForeignKey("MaDichVu")]
        public virtual DichVu DichVu { get; set; }
    }
}
