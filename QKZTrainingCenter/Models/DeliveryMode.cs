
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QKZTrainingCenter.Models
{
    [Table("DeliveryMode")]
    public class DeliveryMode
    {
        [Key]
        public int DeliveryModeID { get; set; }

        [Required]
        public string ModeName { get; set; } = string.Empty;
    }
}
