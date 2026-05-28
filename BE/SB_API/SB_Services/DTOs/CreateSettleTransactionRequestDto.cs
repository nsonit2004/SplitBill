namespace SB_Services.DTOs
{
    public class CreateSettleTransactionRequestDto
    {
        public string DebtorId { get; set; } = string.Empty;
        public string CreditorId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        
        // PaymentMethod: "VietQR" | "Cash"
        public string PaymentMethod { get; set; } = "VietQR";
    }
}
