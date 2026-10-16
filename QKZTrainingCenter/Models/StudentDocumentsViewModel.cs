namespace QKZTrainingCenter.Models
{
    public class StudentDocumentsViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public string? IDDocumentPath { get; set; }

        public bool HasIDDocument =>
            !string.IsNullOrWhiteSpace(IDDocumentPath);

        public string? ProofOfPaymentPath { get; set; }

        public bool HasProofOfPayment =>
            !string.IsNullOrWhiteSpace(ProofOfPaymentPath);

        public string PaymentStatus { get; set; } = "Not Submitted";
    }
}