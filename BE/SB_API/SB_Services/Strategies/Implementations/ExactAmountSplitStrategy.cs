using System;
using System.Collections.Generic;
using System.Linq;
using SB_Services.DTOs;

namespace SB_Services.Strategies.Implementations
{
    public class ExactAmountSplitStrategy : ISplitStrategy
    {
        public string MethodName => "Amount";

        public Dictionary<string, decimal> CalculateSplit(decimal totalAmount, List<string> memberIds, List<SplitValueDto> splitValues)
        {
            var result = new Dictionary<string, decimal>();
            if (splitValues == null || !splitValues.Any())
                throw new ArgumentException("Danh sách phân bổ số tiền không được trống.");

            decimal sum = splitValues.Sum(v => v.Value);
            decimal discrepancy = totalAmount - sum;

            // Chấp nhận sai số làm tròn cực nhỏ (dưới 1.00 đơn vị tiền tệ), nếu lớn hơn thì báo lỗi
            if (Math.Abs(discrepancy) > 1.00M)
            {
                throw new ArgumentException("Tổng số tiền chia lẻ không khớp với tổng hóa đơn.");
            }

            bool adjusted = false;
            foreach (var memberId in memberIds)
            {
                var valDto = splitValues.FirstOrDefault(v => v.MemberId == memberId);
                decimal owed = valDto?.Value ?? 0;
                
                // Bù trừ sai số lẻ vào người đầu tiên tìm thấy
                if (!adjusted && valDto != null)
                {
                    owed += discrepancy;
                    adjusted = true;
                }
                
                result[memberId] = owed;
            }

            return result;
        }
    }
}
