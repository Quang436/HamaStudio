using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("Portfolio")]
    public class Portfolio
    {
        [Key]
        public int MaAnh { get; set; }

        public int? MaDichVu { get; set; }

        [Required]
        [StringLength(255)]
        public string LinkFileAnh { get; set; }

        [StringLength(200)]
        public string TieuDe { get; set; }

        [ForeignKey("MaDichVu")]
        public virtual DichVu DichVu { get; set; }
    }
}
