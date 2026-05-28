using System.Collections.Generic;

namespace SB_Services.DTOs
{
    public class GroupAnalyticsDto
    {
        public decimal TotalSpending { get; set; }
        public int TotalExpenses { get; set; }
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
        public List<MemberSpendingDto> TopSpenders { get; set; } = new();
    }

    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = "Other";
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class MemberSpendingDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
    }
}
