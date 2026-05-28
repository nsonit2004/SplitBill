using System;

namespace SB_Services.DTOs
{
    public class SettleTransactionResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        
        public string DebtorId { get; set; } = string.Empty;
        public string DebtorNickname { get; set; } = string.Empty;
        
        public string CreditorId { get; set; } = string.Empty;
        public string CreditorNickname { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "VietQR";
        public string PaymentStatus { get; set; } = "Pending";
        public string? TransferReference { get; set; }
        
        public string? ProofImageUrl { get; set; }
        public DateTime? BankVerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public string? VietQrUrl { get; set; }
        
        public string? BankCode { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankAccountName { get; set; }
    }
}
