using System;
using System.Collections.Generic;

namespace SB_Services.DTOs
{
    public class ExpenseResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string SplitMethod { get; set; } = "Equally";
        public string? ImageUrl { get; set; }
        public string Category { get; set; } = "Other";
        public string? CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<ExpensePayerDto> Payers { get; set; } = new List<ExpensePayerDto>();
        public List<ExpenseSliceDto> Slices { get; set; } = new List<ExpenseSliceDto>();
    }

    public class ExpensePayerDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
    }

    public class ExpenseSliceDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
    }
}
