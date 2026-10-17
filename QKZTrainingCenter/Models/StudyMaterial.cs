using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("StudyMaterial")]
    public class StudyMaterial
    {
        [Key]
        public int MaterialID { get; set; }

        [Required]
        public int CourseID { get; set; }

        [Required]
        public int UploadedByUserID { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileURL { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FileType { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}