namespace SB_Services.DTOs
{
    public class MemberBalanceDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public bool IsVirtual { get; set; }
        
        public decimal PaidInExpenses { get; set; }
        public decimal OwedInExpenses { get; set; }
        public decimal SettledPaid { get; set; }
        public decimal SettledReceived { get; set; }
        
        public decimal NetBalance { get; set; } // Số dư hiện tại = (PaidInExpenses - OwedInExpenses) + (SettledPaid - SettledReceived)
    }
}
