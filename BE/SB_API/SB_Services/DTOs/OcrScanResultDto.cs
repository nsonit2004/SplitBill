using System.Collections.Generic;

namespace SB_Services.DTOs
{
    public class OcrScanResultDto
    {
        public string MerchantName { get; set; } = string.Empty;
        public string? Date { get; set; }
        public decimal Tax { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public string Category { get; set; } = "Other";
        public List<OcrLineItemDto> Items { get; set; } = new List<OcrLineItemDto>();
    }

    public class OcrLineItemDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
