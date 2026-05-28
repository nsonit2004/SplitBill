namespace SB_Services.DTOs
{
    public class BankAccountVerificationResultDto
    {
        public bool IsVerified { get; set; }
        public string? ResolvedAccountName { get; set; }
        public string? Message { get; set; }
        public string Provider { get; set; } = "Mock";
    }
}
