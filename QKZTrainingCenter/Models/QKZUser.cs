using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("Users")]
    public class QKZUser
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required]
        public int RoleID { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }
    }
}