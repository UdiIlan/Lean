﻿/*
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
using NUnit.Framework;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework
{
    [TestFixture]
    public class QCAlgorithmFrameworkTests
    {
        [Test]
        public void SetsInsightGeneratedAndCloseTimes()
        {
            var eventFired = false;
            var algo = new QCAlgorithmFramework();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            algo.InsightsGenerated += (algorithm, data) =>
            {
                eventFired = true;
                var insights = data.Insights;
                Assert.AreEqual(1, insights.Count);
                Assert.IsTrue(insights.All(insight => insight.GeneratedTimeUtc != default(DateTime)));
                Assert.IsTrue(insights.All(insight => insight.CloseTimeUtc != default(DateTime)));
            };
            var security = algo.AddEquity("SPY");
            algo.SetUniverseSelection(new ManualUniverseSelectionModel());

            var alpha = new FakeAlpha();
            algo.SetAlpha(alpha);

            var construction = new FakePortfolioConstruction();
            algo.SetPortfolioConstruction(construction);

            var tick = new Tick
            {
                Symbol = security.Symbol,
                Value = 1,
                Quantity = 2
            };
            security.SetMarketPrice(tick);

            algo.OnFrameworkData(new Slice(new DateTime(2000, 01, 01), algo.Securities.Select(s => tick)));

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, construction.Insights.Count);
            Assert.IsTrue(construction.Insights.All(insight => insight.GeneratedTimeUtc != default(DateTime)));
            Assert.IsTrue(construction.Insights.All(insight => insight.CloseTimeUtc != default(DateTime)));
        }

        class FakeAlpha : AlphaModel
        {
            public override IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
            {
                yield return Insight.Price(Symbols.SPY, TimeSpan.FromDays(1), InsightDirection.Up, .5, .75);
            }
        }

        class FakePortfolioConstruction : PortfolioConstructionModel
        {
            public IReadOnlyCollection<Insight> Insights { get; private set; }
            public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithmFramework algorithm, Insight[] insights)
            {
                Insights = insights;
                return insights.Select(insight => PortfolioTarget.Percent(algorithm, insight.Symbol, 0.01m));
            }
        }
    }
}
