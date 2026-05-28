namespace SB_Services.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? BankCode { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankAccountName { get; set; }
        public bool BankAccountVerified { get; set; }
    }
}
