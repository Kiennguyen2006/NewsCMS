using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsCMS.Models
{
    [Table("VaiTro")]
    public class VaitroModel
    {
        [Key]
        public int MaVaiTro { get; set; }

        [Required]
        public string TenVaiTro { get; set; }

        public string? MoTa { get; set; }
    }
}