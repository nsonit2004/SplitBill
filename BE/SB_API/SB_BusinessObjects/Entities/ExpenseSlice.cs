using System;

namespace SB_BusinessObjects.Entities
{
    public class ExpenseSlice
    {
        public string ExpenseId { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }

        // Navigation Properties
        public virtual Expense Expense { get; set; } = null!;
        public virtual GroupMember Member { get; set; } = null!;
    }
}
