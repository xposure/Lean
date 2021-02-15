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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of payments for cash dividends in backtesting. When data normalization mode is set
    /// to "Raw" the dividends are paid as cash directly into your portfolio.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="data event handlers" />
    /// <meta name="tag" content="dividend event" />
    public class TickerChangeAndSplitAlgorithm : QCAlgorithm
    {
        private string _ticker = "imux";
        readonly DateTime _expectedTickerChangeDate = new DateTime(2019, 04, 15);
        private bool _tickerChanged = false;
        private Symbol _symbol;
        private Identity _price;

        public override void Initialize()
        {
            SetStartDate(2019, 04, 01);
            SetEndDate(2019, 05, 01);

            SetCash(100000);

            _symbol = AddEquity(_ticker, Resolution.Daily, Market.USA).Symbol;
            _price = Identity(_symbol);

            PlotIndicator($"{_symbol.Value} Price", _price);
        }

        public override void OnData(Slice slice)
        {
            // Check OnDAta is receiving prices for this security
            if (!slice.ContainsKey(_symbol))
            {
                throw new Exception("We should receive data for this security!");
            }

            Log($"\n\t {slice.Bars[_symbol]}");
            
            // Check ticker change is correctly handled.
            if (slice.SymbolChangedEvents.Any())
            {
                Log($"\n\tTicker changed on {Time:u} | {_symbol.ID} | " +
                    $"{slice.SymbolChangedEvents[_symbol].OldSymbol} => {slice.SymbolChangedEvents[_symbol].NewSymbol}\n");
                _tickerChanged = true;

            }
            else if (Time > _expectedTickerChangeDate && !_tickerChanged)
            {
                throw new Exception("Ticker change not handled correctly!");
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_symbol, 0.1);
                Log($"Purchased Security {_symbol.ID}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var securityChange in changes.AddedSecurities)
            {
                Log($"\n\tAdded security {_symbol.ID} on {Time:u} | Ticker [{securityChange.Symbol.Value}] | Price {securityChange.Close}\n");
            }
        }

        public void OnData(Splits data)
        {
            var split = data[_ticker];
            Log($"{split.Time.ToIso8601Invariant()} >> SPLIT >> {split.Symbol} - " +
                $"{split.SplitFactor.ToStringInvariant()} - " +
                $"{Portfolio.Cash.ToStringInvariant()} - " +
                $"{Portfolio[_ticker].Quantity.ToStringInvariant()}"
            );
        }

        public override void OnEndOfAlgorithm()
        {
            Liquidate();
        }
    }
}