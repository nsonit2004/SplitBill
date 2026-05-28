using System;

namespace SB_Services.DTOs
{
    public class GroupMemberDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public bool IsVirtual { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
