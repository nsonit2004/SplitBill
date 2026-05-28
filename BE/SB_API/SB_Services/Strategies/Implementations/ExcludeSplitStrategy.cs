using System;
using System.Collections.Generic;
using System.Linq;
using SB_Services.DTOs;

namespace SB_Services.Strategies.Implementations
{
    public class ExcludeSplitStrategy : ISplitStrategy
    {
        public string MethodName => "Exclude";

        public Dictionary<string, decimal> CalculateSplit(decimal totalAmount, List<string> memberIds, List<SplitValueDto> splitValues)
        {
            var result = new Dictionary<string, decimal>();
            if (splitValues == null || !splitValues.Any())
                throw new ArgumentException("Danh sách phân bổ loại trừ không được trống.");

            // Lọc ra danh sách các thành viên được chia (Value = 1 có nghĩa là được chia)
            var includedMemberIds = splitValues
                .Where(v => v.Value > 0)
                .Select(v => v.MemberId)
                .ToList();

            if (!includedMemberIds.Any())
                throw new ArgumentException("Phải chọn ít nhất một thành viên để chia tiền.");

            int count = includedMemberIds.Count;
            decimal baseAmount = Math.Round(totalAmount / count, 2);
            decimal sumOfRounded = baseAmount * count;
            decimal remainder = totalAmount - sumOfRounded;

            for (int i = 0; i < includedMemberIds.Count; i++)
            {
                decimal amount = (i == 0) ? baseAmount + remainder : baseAmount;
                result[includedMemberIds[i]] = amount;
            }

            // Gán 0 cho các thành viên bị loại trừ
            foreach (var memberId in memberIds)
            {
                if (!result.ContainsKey(memberId))
                {
                    result[memberId] = 0;
                }
            }

            return result;
        }
    }
}
