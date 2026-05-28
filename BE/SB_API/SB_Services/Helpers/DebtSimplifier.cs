using System;
using System.Collections.Generic;
using System.Linq;
using SB_Services.DTOs;

namespace SB_Services.Helpers
{
    public static class DebtSimplifier
    {
        /// <summary>
        /// Rút gọn các giao dịch nợ giữa các thành viên.
        /// </summary>
        /// <param name="memberBalances">Từ điển ánh xạ MemberId -> Net Balance (Số tiền trả - Số tiền nợ)</param>
        /// <returns>Danh sách các giao dịch thanh toán tối giản</returns>
        public static List<SimplifiedDebtDto> Simplify(Dictionary<string, decimal> memberBalances)
        {
            var transactions = new List<SimplifiedDebtDto>();

            // Lọc ra các con nợ và chủ nợ, bỏ qua chênh lệch cực nhỏ do làm tròn (< 0.01)
            var debtors = memberBalances
                .Where(x => x.Value < -0.01M)
                .Select(x => new MemberBalance(x.Key, Math.Abs(x.Value)))
                .OrderByDescending(x => x.Balance)
                .ToList();

            var creditors = memberBalances
                .Where(x => x.Value > 0.01M)
                .Select(x => new MemberBalance(x.Key, x.Value))
                .OrderByDescending(x => x.Balance)
                .ToList();

            int debtorIndex = 0;
            int creditorIndex = 0;

            while (debtorIndex < debtors.Count && creditorIndex < creditors.Count)
            {
                var debtor = debtors[debtorIndex];
                var creditor = creditors[creditorIndex];

                // Số tiền thanh toán là giá trị nhỏ nhất giữa nợ phải trả và khoản được nhận
                decimal settleAmount = Math.Min(debtor.Balance, creditor.Balance);
                settleAmount = Math.Round(settleAmount, 2);

                if (settleAmount > 0)
                {
                    transactions.Add(new SimplifiedDebtDto
                    {
                        DebtorId = debtor.MemberId,
                        CreditorId = creditor.MemberId,
                        Amount = settleAmount
                    });

                    debtor.Balance -= settleAmount;
                    creditor.Balance -= settleAmount;
                }

                // Chuyển sang người tiếp theo nếu đã giải quyết xong nợ/khoản nhận
                if (debtor.Balance < 0.01M)
                {
                    debtorIndex++;
                }
                if (creditor.Balance < 0.01M)
                {
                    creditorIndex++;
                }
            }

            return transactions;
        }

        private class MemberBalance
        {
            public string MemberId { get; }
            public decimal Balance { get; set; }

            public MemberBalance(string memberId, decimal balance)
            {
                MemberId = memberId;
                Balance = balance;
            }
        }
    }
}
