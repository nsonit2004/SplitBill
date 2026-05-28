using System.Collections.Generic;
using SB_Services.DTOs;

namespace SB_Services.Strategies
{
    public interface ISplitStrategy
    {
        string MethodName { get; }
        
        /// <summary>
        /// Tính toán số tiền nợ cho mỗi thành viên dựa trên chiến lược chia tiền.
        /// </summary>
        /// <returns>Từ điển ánh xạ MemberId -> Số tiền nợ</returns>
        Dictionary<string, decimal> CalculateSplit(decimal totalAmount, List<string> memberIds, List<SplitValueDto> splitValues);
    }
}
