using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("ComboPackageCourses")]
    public class ComboPackageCourse
    {
        [Key]
        public int ComboPackageCourseID { get; set; }

        [Required]
        public int PackageID { get; set; }

        [Required]
        public int CourseID { get; set; }
    }
}