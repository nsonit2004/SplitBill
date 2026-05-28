using System;
using System.Collections.Generic;

namespace SB_BusinessObjects.Entities
{
    public class Expense
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? ImageUrl { get; set; }
        
        // SplitMethod: "Equally" | "Amount" | "Exclude" | "Shares"
        public string SplitMethod { get; set; } = "Equally";
        
        // Category: "Food" | "Transport" | "Accommodation" | "Entertainment" | "Shopping" | "Other"
        public string? Category { get; set; } = "Other";
        
        public string? CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Group Group { get; set; } = null!;
        public virtual User? CreatedBy { get; set; }

        public virtual ICollection<ExpensePayer> Payers { get; set; } = new List<ExpensePayer>();
        public virtual ICollection<ExpenseSlice> Slices { get; set; } = new List<ExpenseSlice>();
    }
}
