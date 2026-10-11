using System;

namespace QKZTrainingCenter.Models
{
    public class StudentRegistrationDetailsViewModel
    {
        public int RegistrationID { get; set; }

        public int? PaymentID { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;

        public string IDNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;

        public string CourseTitle { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; }

        public string RegistrationStatus { get; set; } = string.Empty;

        public string? PaymentStatus { get; set; }

        public string? ProofOfPayment { get; set; }
    }
}