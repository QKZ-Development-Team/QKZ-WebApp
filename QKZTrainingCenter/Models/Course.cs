using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("Courses")]
    public class Course
    {
        [Key]
        public int CourseID { get; set; }

        public string? CourseName { get; set; }

        public string? Title { get; set; }

        public string? CourseDescription { get; set; }

        public string? CourseType { get; set; }

        public string? Duration { get; set; }

        public decimal? CourseFee { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}