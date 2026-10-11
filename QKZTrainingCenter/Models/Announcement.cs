
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("Announcement")]
    public class Announcement
    {
        [Key]
        public int AnnouncementID { get; set; }

        public int? CourseID { get; set; }

        [Required]
        public int AuthorUserID { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime PublishDate { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}