using System;
using System.Collections.Generic;

namespace SB_BusinessObjects.Entities
{
    public class GroupMember
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        
        public string Nickname { get; set; } = string.Empty;
        public bool IsVirtual { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Group Group { get; set; } = null!;
        public virtual User? User { get; set; }

        public virtual ICollection<ExpensePayer> ExpensesPaid { get; set; } = new List<ExpensePayer>();
        public virtual ICollection<ExpenseSlice> ExpensesOwed { get; set; } = new List<ExpenseSlice>();
        
        public virtual ICollection<SettleTransaction> SettleTransactionsAsDebtor { get; set; } = new List<SettleTransaction>();
        public virtual ICollection<SettleTransaction> SettleTransactionsAsCreditor { get; set; } = new List<SettleTransaction>();
    }
}
