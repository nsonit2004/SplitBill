using System;

namespace SB_Services.DTOs
{
    public class GroupInviteResponseDto
    {
        public string InviteToken { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int MaxUses { get; set; }
        public int UsedCount { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
