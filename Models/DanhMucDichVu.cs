using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamaStudio.Models
{
    [Table("DanhMucDichVu")]
    public class DanhMucDichVu
    {
        [Key]
        public int MaDanhMuc { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDanhMuc { get; set; }

        public virtual ICollection<DichVu> DichVus { get; set; }
    }
}
