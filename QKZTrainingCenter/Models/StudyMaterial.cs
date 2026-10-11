using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Hosting;
using QKZTrainingCenter.Models;

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
        public string Title { get; set; } = string.Empty;

        [Required]
        public string FileURL { get; set; } = string.Empty;

        [Required]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public DateTime UploadDate { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}