using System.Collections.Generic;

namespace SB_Services.DTOs
{
    public class CreateExpenseRequestDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        
        // SplitMethod: "Equally" | "Amount" | "Exclude" | "Shares"
        public string SplitMethod { get; set; } = "Equally";
        
        public string? ImageUrl { get; set; }
        
        // Category: "Food" | "Transport" | "Accommodation" | "Entertainment" | "Shopping" | "Other"
        public string Category { get; set; } = "Other";

        public List<PayerInputDto> Payers { get; set; } = new List<PayerInputDto>();
        public List<SliceInputDto> Slices { get; set; } = new List<SliceInputDto>();
    }

    public class PayerInputDto
    {
        public string MemberId { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
    }

    public class SliceInputDto
    {
        public string MemberId { get; set; } = string.Empty;
        
        // Giá trị dùng để chia:
        // - Với Equally: không cần truyền hoặc truyền gì cũng được
        // - Với Amount: Số tiền chính xác mà người này phải trả
        // - Với Exclude: 1 (nếu chia), 0 (nếu loại trừ)
        // - Với Shares: Số phần (ví dụ: 1, 2, 3...)
        public decimal Value { get; set; }
    }
}
