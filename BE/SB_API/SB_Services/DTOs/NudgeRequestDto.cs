namespace SB_Services.DTOs
{
    public class NudgeRequestDto
    {
        public string DebtorId { get; set; } = string.Empty;
        public string CreditorId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
