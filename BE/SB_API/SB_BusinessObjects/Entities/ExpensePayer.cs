using System;

namespace SB_BusinessObjects.Entities
{
    public class ExpensePayer
    {
        public string ExpenseId { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }

        // Navigation Properties
        public virtual Expense Expense { get; set; } = null!;
        public virtual GroupMember Member { get; set; } = null!;
    }
}
