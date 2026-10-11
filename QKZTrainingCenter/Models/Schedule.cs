using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("Schedule")]
    public class Schedule
    {
        [Key]
        public int ScheduleID { get; set; }

        [Required]
        public int CourseID { get; set; }

        [Required]
        public int DeliveryModeID { get; set; }

        public int? VenueID { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}