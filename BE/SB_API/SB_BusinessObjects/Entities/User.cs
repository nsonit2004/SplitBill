using System;
using System.Collections.Generic;

namespace SB_BusinessObjects.Entities
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        
        // Ngân hàng phục vụ sinh VietQR
        public string? BankCode { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankAccountName { get; set; }
        public bool BankAccountVerified { get; set; } = false;
        public DateTime? BankAccountVerifiedAt { get; set; }
        public string? BankVerificationProvider { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupInvite> CreatedGroupInvites { get; set; } = new List<GroupInvite>();
    }
}
