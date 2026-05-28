using System;
using System.Collections.Generic;
using SB_Services.DTOs;

namespace SB_Services.Strategies.Implementations
{
    public class EquallySplitStrategy : ISplitStrategy
    {
        public string MethodName => "Equally";

        public Dictionary<string, decimal> CalculateSplit(decimal totalAmount, List<string> memberIds, List<SplitValueDto> splitValues)
        {
            var result = new Dictionary<string, decimal>();
            if (memberIds == null || memberIds.Count == 0)
                return result;

            int count = memberIds.Count;
            // Làm tròn đến 2 chữ số thập phân (hoặc 0 nếu là VND thực tế, nhưng dùng 2 số lẻ để tương thích database)
            decimal baseAmount = Math.Round(totalAmount / count, 2);
            decimal sumOfRounded = baseAmount * count;
            decimal remainder = totalAmount - sumOfRounded;

            for (int i = 0; i < count; i++)
            {
                // Người đầu tiên sẽ gánh phần tiền lẻ dư ra do làm tròn (lỗi chia lẻ tiền)
                decimal amount = (i == 0) ? baseAmount + remainder : baseAmount;
                result[memberIds[i]] = amount;
            }

            return result;
        }
    }
}
