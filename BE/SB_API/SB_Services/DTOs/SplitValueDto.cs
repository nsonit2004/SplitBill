namespace SB_Services.DTOs
{
    public class SplitValueDto
    {
        public string MemberId { get; set; } = string.Empty;
        public decimal Value { get; set; } // Số tiền chính xác hoặc số phần (shares)
    }
}
