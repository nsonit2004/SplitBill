using System;
using System.Collections.Generic;
using System.Linq;
using SB_Services.DTOs;

namespace SB_Services.Strategies.Implementations
{
    public class BySharesSplitStrategy : ISplitStrategy
    {
        public string MethodName => "Shares";

        public Dictionary<string, decimal> CalculateSplit(decimal totalAmount, List<string> memberIds, List<SplitValueDto> splitValues)
        {
            var result = new Dictionary<string, decimal>();
            if (splitValues == null || !splitValues.Any())
                throw new ArgumentException("Danh sách chia theo phần (Shares) không được trống.");

            decimal totalShares = splitValues.Sum(v => v.Value);
            if (totalShares <= 0)
                throw new ArgumentException("Tổng số phần (Shares) phải lớn hơn 0.");

            decimal sumOfRounded = 0;
            var tempResult = new Dictionary<string, decimal>();

            foreach (var memberId in memberIds)
            {
                var valDto = splitValues.FirstOrDefault(v => v.MemberId == memberId);
                decimal shares = valDto?.Value ?? 0;
                
                decimal owed = Math.Round((shares / totalShares) * totalAmount, 2);
                tempResult[memberId] = owed;
                sumOfRounded += owed;
            }

            decimal remainder = totalAmount - sumOfRounded;

            // Bù trừ phần tiền lẻ chênh lệch vào người có số phần nhiều nhất (hoặc người đầu tiên)
            var targetMember = splitValues
                .OrderByDescending(v => v.Value)
                .Select(v => v.MemberId)
                .FirstOrDefault() ?? memberIds.First();

            foreach (var memberId in memberIds)
            {
                decimal amount = tempResult[memberId];
                if (memberId == targetMember)
                {
                    amount += remainder;
                }
                result[memberId] = amount;
            }

            return result;
        }
    }
}
