using System.ComponentModel.DataAnnotations;

namespace QKZTrainingCenter.Models
{
    public class ComboPackage
    {
        [Key]
        public int PackageID { get; set; }

        [Required]
        [StringLength(100)]
        public string PackageName { get; set; } = string.Empty;

        [Required]
        public decimal PackagePrice { get; set; }

        public bool IsActive { get; set; } = true;
    }
}