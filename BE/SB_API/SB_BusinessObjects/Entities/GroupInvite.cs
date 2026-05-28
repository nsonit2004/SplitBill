using System;

namespace SB_BusinessObjects.Entities
{
    public class GroupInvite
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public int MaxUses { get; set; } = 1;
        public int UsedCount { get; set; } = 0;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Group Group { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
    }
}
