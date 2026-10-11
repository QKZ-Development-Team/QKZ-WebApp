using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        public int StudentID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public string IDNumber { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }
    }
}