using System;

namespace SB_Services.DTOs
{
    public class BankTransferWebhookDto
    {
        public string TransferReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? BankCode { get; set; }
        public string? BankAccountNo { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }
}
