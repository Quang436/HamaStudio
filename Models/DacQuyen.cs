using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("DacQuyen")]
    public class DacQuyen
    {
        [Key]
        public int MaDacQuyen { get; set; }

        public int? MaDichVu { get; set; }

        [StringLength(50)]
        public string Icon { get; set; }

        [Required]
        [StringLength(100)]
        public string TieuDe { get; set; }

        [Required]
        [StringLength(200)]
        public string NoiDung { get; set; }

        [ForeignKey("MaDichVu")]
        public virtual DichVu DichVu { get; set; }
    }
}
