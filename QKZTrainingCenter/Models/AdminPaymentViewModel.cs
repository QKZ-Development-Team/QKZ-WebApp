namespace QKZTrainingCenter.Models
{
    public class AdminPaymentViewModel
    {
        public int PaymentID { get; set; }

        public int RegistrationID { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string TransactionReference { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? ProofOfPayment { get; set; }
    }
}