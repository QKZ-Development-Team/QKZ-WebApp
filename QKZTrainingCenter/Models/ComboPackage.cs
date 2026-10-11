using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("ComboPackages")]
    public class ComboPackage
    {
        [Key]
        public int PackageID { get; set; }

        [Required]
        public string PackageName { get; set; } = string.Empty;

        [Required]
        public decimal PackagePrice { get; set; }

        public bool IsActive { get; set; }
    }
}