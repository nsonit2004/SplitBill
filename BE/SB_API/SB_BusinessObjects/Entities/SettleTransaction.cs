using System;

namespace SB_BusinessObjects.Entities
{
    public class SettleTransaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public string DebtorId { get; set; } = string.Empty;
        public string CreditorId { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        
        // PaymentMethod: "VietQR" | "Cash"
        public string PaymentMethod { get; set; } = "VietQR";
        
        // PaymentStatus: "Pending" | "Completed" | "Failed"
        public string PaymentStatus { get; set; } = "Pending";
        public string? TransferReference { get; set; }
        
        public string? ProofImageUrl { get; set; }
        public DateTime? BankVerifiedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Group Group { get; set; } = null!;
        public virtual GroupMember Debtor { get; set; } = null!;
        public virtual GroupMember Creditor { get; set; } = null!;
    }
}
