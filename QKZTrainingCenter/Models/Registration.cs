using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("Registrations")]
    public class Registration
    {
        [Key]
        public int RegistrationID { get; set; }

        [Required]
        public int StudentID { get; set; }

        public int? CourseID { get; set; }

        public int? PackageID { get; set; }

        public int? VenueID { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; }

        [Required]
        public string RegistrationStatus { get; set; } = string.Empty;
    }
}