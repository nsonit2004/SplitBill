using System;
using System.Collections.Generic;
using System.Linq;

namespace SB_Services.Strategies
{
    public class SplitStrategyFactory
    {
        private readonly IEnumerable<ISplitStrategy> _strategies;

        public SplitStrategyFactory(IEnumerable<ISplitStrategy> strategies)
        {
            _strategies = strategies;
        }

        public ISplitStrategy GetStrategy(string methodName)
        {
            var strategy = _strategies.FirstOrDefault(s => s.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            if (strategy == null)
            {
                throw new ArgumentException($"Phương thức phân chia '{methodName}' không được hỗ trợ.");
            }
            return strategy;
        }
    }
}
