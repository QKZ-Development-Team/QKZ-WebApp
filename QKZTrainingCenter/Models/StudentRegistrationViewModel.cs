using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QKZTrainingCenter.Models
{
    public class StudentRegistrationViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [Display(Name = "South African ID Number")]
        public string SouthAfricanId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Upload ID Document")]
        public IFormFile? IDDocument { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Grade")]
        public string? Grade { get; set; }

        public int? PackageID { get; set; }

        [Required]
        [Display(Name = "Training Option")]
        public string TrainingOption { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Choose Your Training")]
        public string TrainingSelection { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public List<ComboPackage> ComboPackages { get; set; }
            = new List<ComboPackage>();
    }
}