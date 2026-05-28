using System;
using System.Collections.Generic;

namespace SB_Services.DTOs
{
    public class GroupDetailResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<GroupMemberDto> Members { get; set; } = new List<GroupMemberDto>();

        public decimal TotalSpent { get; set; }
        public decimal UserNetBalance { get; set; }
    }
}
