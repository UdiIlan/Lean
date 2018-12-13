/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test for consistency of hour data over a reverse split event in US equities.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="regression test" />
    public class HourSplitRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private bool _receivedWarningEvent;
        private bool _receivedOccurredEvent;
        private int _dataCount;

        public override void Initialize()
        {
            SetStartDate(2005, 2, 25);
            SetEndDate(2005, 2, 28);
            SetCash(100000);
            SetBenchmark(x => 0);

            _symbol = AddEquity("AAPL", Resolution.Hour).Symbol;
        }

        public void OnData(TradeBars tradeBars)
        {
            TradeBar bar;
            if (!tradeBars.TryGetValue(_symbol, out bar)) return;

            if (!Portfolio.Invested && Time.Date == EndDate.Date)
            {
                Buy(_symbol, 1);
            }
        }

        public override void OnData(Slice slice)
        {
            _dataCount += slice.Bars.Count;
            if (slice.Splits.Any())
            {
                if (slice.Splits.Single().Value.Type == SplitType.Warning)
                {
                    _receivedWarningEvent = true;
                    Debug($"{slice.Splits.Single().Value}");
                }
                else if (slice.Splits.Single().Value.Type == SplitType.SplitOccurred)
                {
                    _receivedOccurredEvent = true;
                    if (slice.Splits.Single().Value.Price != 88.9700m || slice.Splits.Single().Value.ReferencePrice != 88.9700m)
                    {
                        throw new Exception("Did not receive expected price values");
                    }
                    Debug($"{slice.Splits.Single().Value}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_receivedOccurredEvent)
            {
                throw new Exception("Did not receive expected split event");
            }
            if (!_receivedWarningEvent)
            {
                throw new Exception("Did not receive expected split warning event");
            }
            if (_dataCount != 14)
            {
                throw new Exception($"Unexpected data count {_dataCount}. Expected 14");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.096%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.001%"},
            {"Sharpe Ratio", "-11.225"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"}
        };
    }
}
