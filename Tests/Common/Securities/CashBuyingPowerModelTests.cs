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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Engine;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashBuyingPowerModelTests
    {
        private Crypto _btcusd;
        private Crypto _btceur;
        private Crypto _ethusd;
        private Crypto _ethbtc;
        private SecurityPortfolioManager _portfolio;
        private BacktestingTransactionHandler _transactionHandler;
        private BacktestingBrokerage _brokerage;
        private CashBuyingPowerModel _buyingPowerModel;
        private QCAlgorithm _algorithm;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _portfolio = _algorithm.Portfolio;
            _portfolio.CashBook.Add("EUR", 0, 1.20m);
            _portfolio.CashBook.Add("BTC", 0, 15000m);
            _portfolio.CashBook.Add("ETH", 0, 1000m);

            _algorithm.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            _transactionHandler = new BacktestingTransactionHandler();
            _brokerage = new BacktestingBrokerage(_algorithm);
            _transactionHandler.Initialize(_algorithm, _brokerage, new TestResultHandler());
            new Thread(_transactionHandler.Run) { IsBackground = true }.Start();

            _algorithm.Transactions.SetOrderProcessor(_transactionHandler);

            var tz = TimeZones.NewYork;
            _btcusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook[CashBook.AccountCurrency],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("BTCUSD", "USD", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance
            );

            _ethusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook[CashBook.AccountCurrency],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.ETHUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("ETHUSD", "USD", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance
            );

            _btceur = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook["EUR"],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCEUR, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("BTCEUR", "EUR", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance
            );

            _ethbtc = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                _portfolio.CashBook["BTC"],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.ETHBTC, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("ETHBTC", "BTC", 1, 0.00001m, 0.00000001m),
                ErrorCurrencyConverter.Instance
            );

            _buyingPowerModel = new CashBuyingPowerModel();
        }

        [TearDown]
        public void TearDown()
        {
            _transactionHandler.Exit();
        }

        [Test]
        public void InitializesCorrectly()
        {
            Assert.AreEqual(1m, _buyingPowerModel.GetLeverage(_btcusd));
            Assert.AreEqual(0m, _buyingPowerModel.GetReservedBuyingPowerForPosition(_btcusd));
        }

        [Test]
        public void SetLeverageDoesNotUpdateLeverage()
        {
            // leverage cannot be set, will always be 1x
            _buyingPowerModel.SetLeverage(_btcusd, 50m);
            Assert.AreEqual(1m, _buyingPowerModel.GetLeverage(_btcusd));
        }

        [Test]
        public void LimitBuyBtcWithUsdRequiresUsdInPortfolio()
        {
            _portfolio.SetCash(20000);

            // Available cash = 20000 USD, can buy 2 BTC at 10000
            var order = new LimitOrder(_btcusd.Symbol, 2m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // Available cash = 20000 USD, cannot buy 2.1 BTC at 10000, need 21000
            order = new LimitOrder(_btcusd.Symbol, 2.1m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // Available cash = 20000 USD, cannot buy 2 BTC at 11000, need 22000
            order = new LimitOrder(_btcusd.Symbol, 2m, 11000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void LimitBuyBtcWithEurRequiresEurInPortfolio()
        {
            _portfolio.SetCash("EUR", 20000m, 1.20m);

            // Available cash = 20000 EUR, can buy 2 BTC at 10000
            var order = new LimitOrder(_btceur.Symbol, 2m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);

            // Available cash = 20000 EUR, cannot buy 2.1 BTC at 10000, need 21000
            order = new LimitOrder(_btceur.Symbol, 2.1m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);

            // Available cash = 20000 EUR, cannot buy 2 BTC at 11000, need 22000
            order = new LimitOrder(_btceur.Symbol, 2m, 11000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);
        }

        [Test]
        public void LimitSellOrderRequiresBaseCurrencyInPortfolio()
        {
            _portfolio.SetCash(0);
            _portfolio.CashBook["BTC"].SetAmount(0.5m);

            // 0.5 BTC in portfolio, can sell 0.5 BTC at any price
            var order = new LimitOrder(_btcusd.Symbol, -0.5m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 0.5 BTC in portfolio, cannot sell 0.6 BTC at any price
            order = new LimitOrder(_btcusd.Symbol, -0.6m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void LimitBuyOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD buy order decreases available USD (5000 - 1500 = 3500 USD)
            SubmitLimitOrder(_btcusd.Symbol, 0.1m, 15000m);

            // ETHUSD buy order decreases available USD (3500 - 3000 = 500 USD)
            SubmitLimitOrder(_ethusd.Symbol, 3m, 1000m);

            // 500 USD available, can buy 0.05 BTC at 10000
            var order = new LimitOrder(_btcusd.Symbol, 0.05m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 500 USD available, cannot buy 0.06 BTC at 10000
            order = new LimitOrder(_btcusd.Symbol, 0.06m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void LimitSellOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["BTC"].SetAmount(1m);
            _portfolio.CashBook["ETH"].SetAmount(3m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });

            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD sell limit order decreases available BTC (1 - 0.1 = 0.9 BTC)
            SubmitLimitOrder(_btcusd.Symbol, -0.1m, 15000m);

            // ETHUSD sell limit order decreases available ETH (3 - 1 = 2 ETH)
            SubmitLimitOrder(_ethusd.Symbol, -1m, 1000m);

            // ETHBTC buy limit order decreases available BTC (0.9 - 0.1 = 0.8 BTC)
            SubmitLimitOrder(_ethbtc.Symbol, 1m, 0.1m);

            // BTCUSD sell stop order decreases available BTC (0.8 - 0.1 = 0.7 BTC)
            SubmitStopMarketOrder(_btcusd.Symbol, -0.1m, 5000m);

            // 0.7 BTC available, can sell 0.7 BTC at any price
            var order = new LimitOrder(_btcusd.Symbol, -0.7m, 10000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 0.7 BTC available, cannot sell 0.8 BTC at any price
            order = new LimitOrder(_btcusd.Symbol, -0.8m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 2 ETH available, can sell 2 ETH at any price
            order = new LimitOrder(_ethusd.Symbol, -2m, 1200m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _ethusd, order).IsSufficient);

            // 2 ETH available, cannot sell 2.1 ETH at any price
            order = new LimitOrder(_ethusd.Symbol, -2.1m, 2000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _ethusd, order).IsSufficient);

            // 0.7 BTC available, can sell stop 0.7 BTC at any price
            var stopOrder = new StopMarketOrder(_btcusd.Symbol, -0.7m, 5000m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, stopOrder).IsSufficient);

            // 0.7 BTC available, cannot sell stop 0.8 BTC at any price
            stopOrder = new StopMarketOrder(_btcusd.Symbol, -0.8m, 5000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, stopOrder).IsSufficient);
        }

        [Test]
        public void MarketBuyBtcWithUsdRequiresUsdInPortfolioPlusFees()
        {
            _portfolio.SetCash(20000);

            _btcusd.SetMarketPrice(new Tick { Value = 10000m });

            // Available cash = 20000 USD, cannot buy 2 BTC at 10000 (fees are excluded)
            var order = new MarketOrder(_btcusd.Symbol, 2m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // Maximum we can market buy with 20000 USD is 1.99401794m BTC
            Assert.AreEqual(1.99401794m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 1).Quantity);

            _btcusd.SetMarketPrice(new Tick { Value = 9900m });

            // Available cash = 20000 USD, can buy 2 BTC at 9900 (plus fees)
            order = new MarketOrder(_btcusd.Symbol, 2m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void MarketBuyBtcWithEurRequiresEurInPortfolioPlusFees()
        {
            _portfolio.SetCash("EUR", 20000m, 1.20m);

            _btceur.SetMarketPrice(new Tick { Value = 10000m });

            // Available cash = 20000 EUR, cannot buy 2 BTC at 10000 (fees are excluded)
            var order = new MarketOrder(_btceur.Symbol, 2m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);

            // Maximum we can market buy with 20000 EUR is 1.99401794m BTC
            var targetValue = 20000m * _portfolio.CashBook["EUR"].ConversionRate / _portfolio.TotalPortfolioValue;
            Assert.AreEqual(1.99401794m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btceur, targetValue).Quantity);

            _btceur.SetMarketPrice(new Tick { Value = 9900m });

            // Available cash = 20000 EUR, can buy 2 BTC at 9900 (plus fees)
            order = new MarketOrder(_btceur.Symbol, 2m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);
        }

        [Test]
        public void MarketSellOrderRequiresBaseCurrencyInPortfolioPlusFees()
        {
            _portfolio.SetCash(0);

            _btcusd.SetMarketPrice(new Tick { Value = 10000m });
            _portfolio.SetCash("BTC", 0.5m, 10000m);

            // 0.5 BTC in portfolio, can sell 0.5 BTC
            var order = new MarketOrder(_btcusd.Symbol, -0.5m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 0.5 BTC in portfolio, cannot sell 0.51 BTC
            order = new MarketOrder(_btcusd.Symbol, -0.51m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // Maximum we can market sell with 0.5 BTC is 0.5 BTC
            Assert.AreEqual(-0.5m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 0).Quantity);
        }

        [Test]
        public void MarketBuyOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD buy order decreases available USD (5000 - 1500 = 3500 USD)
            SubmitLimitOrder(_btcusd.Symbol, 0.1m, 15000m);

            // ETHUSD buy order decreases available USD (3500 - 3000 = 500 USD)
            SubmitLimitOrder(_ethusd.Symbol, 3m, 1000m);

            // Maximum we can market buy with 500 USD is 0.03323363m BTC
            var quantity = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 500 / _portfolio.TotalPortfolioValue).Quantity;
            Assert.AreEqual(0.03323363m, quantity);

            // 500 USD available, can buy `quantity` BTC at 15000
            var order = new MarketOrder(_btcusd.Symbol, quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 500 USD available, cannot buy `quantity` + _btcusd.SymbolProperties.LotSize BTC at 15000
            order = new MarketOrder(_btcusd.Symbol, quantity + _btcusd.SymbolProperties.LotSize, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void MarketSellOrderChecksOpenOrders()
        {
            _portfolio.SetCash(5000);
            _portfolio.CashBook["BTC"].SetAmount(1m);
            _portfolio.CashBook["ETH"].SetAmount(3m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });

            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });
            _algorithm.SetFinishedWarmingUp();

            // BTCUSD sell order decreases available BTC (1 - 0.1 = 0.9 BTC)
            SubmitLimitOrder(_btcusd.Symbol, -0.1m, 15000m);

            // ETHBTC buy order decreases available BTC (0.9 - 0.1 = 0.8 BTC)
            SubmitLimitOrder(_ethbtc.Symbol, 1m, 0.1m);

            // Maximum we can market sell with 0.8 BTC is -0.79760717m BTC (for a target position of 0.2 BTC)
            // target value = (1 - 0.8) * price
            Assert.AreEqual(-0.79760717m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 0.2m * 15000 / _portfolio.TotalPortfolioValue).Quantity);

            // 0.8 BTC available, can sell 0.80 BTC at market
            var order = new MarketOrder(_btcusd.Symbol, -0.80m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 0.8 BTC available, cannot sell 0.81 BTC at market
            order = new MarketOrder(_btcusd.Symbol, -0.81m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void LimitBuyOrderIncludesFees()
        {
            _portfolio.SetCash(20000);
            _btcusd.FeeModel = new ConstantFeeModel(50);

            // Available cash = 20000, cannot buy 2 BTC at 10000 because of order fee
            var order = new LimitOrder(_btcusd.Symbol, 2m, 10000m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // deposit another 50 USD
            _portfolio.CashBook["USD"].AddAmount(50);

            // now the order is allowed
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void CalculatesMaximumOrderQuantityCorrectly()
        {
            _portfolio.SetCash(10000);
            _portfolio.SetCash("EUR", 10000m, 1.20m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _ethusd = _algorithm.AddCrypto("ETHUSD");
            _ethusd.SetMarketPrice(new Tick { Value = 1000m });

            _ethbtc = _algorithm.AddCrypto("ETHBTC");
            _ethbtc.SetMarketPrice(new Tick { Value = 0.1m });

            _btceur = _algorithm.AddCrypto("BTCEUR");
            _btceur.SetMarketPrice(new Tick { Value = 12000m });
            _algorithm.SetFinishedWarmingUp();

            // 0.66467264m * 15000 + fees + price buffer <= 10000 USD
            var targetValue = 10000 / _portfolio.TotalPortfolioValue;

            var getMaximumOrderQuantityForTargetValueResult = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, targetValue).Quantity;
            Assert.AreEqual(0.66467264m, getMaximumOrderQuantityForTargetValueResult);

            var order = new MarketOrder(_btcusd.Symbol, getMaximumOrderQuantityForTargetValueResult, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            // 9.97008973m * 1000 + fees <= 10000 USD
            getMaximumOrderQuantityForTargetValueResult = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _ethusd, targetValue).Quantity;
            Assert.AreEqual(9.97008973m, getMaximumOrderQuantityForTargetValueResult);

            order = new MarketOrder(_ethusd.Symbol, getMaximumOrderQuantityForTargetValueResult, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _ethusd, order).IsSufficient);

            // no BTC in portfolio, cannot buy ETH with BTC
            Assert.AreEqual(0m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _ethbtc, 1).Quantity);

            // 0.83084081m * 12000 + fees <= 10000 EUR
            targetValue = 10000m * _portfolio.CashBook["EUR"].ConversionRate / _portfolio.TotalPortfolioValue;
            getMaximumOrderQuantityForTargetValueResult  = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btceur, targetValue).Quantity;
            Assert.AreEqual(0.83084081m, getMaximumOrderQuantityForTargetValueResult);

            order = new MarketOrder(_btceur.Symbol, getMaximumOrderQuantityForTargetValueResult, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);
        }

        [Test]
        public void CalculatesMaximumOrderQuantityCorrectlySmallerTarget()
        {
            _portfolio.SetCash(10000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _algorithm.SetFinishedWarmingUp();

            var getMaximumOrderQuantityForTargetValueResult = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 0.1m);
            // Quantity * 15000m + fees + price buffer <= 1000 USD Target
            Assert.AreEqual(0.06646726m, getMaximumOrderQuantityForTargetValueResult.Quantity);

            var order = new MarketOrder(_btcusd.Symbol, getMaximumOrderQuantityForTargetValueResult.Quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void CalculatesMaximumOrderQuantityCorrectlyBiggerTarget()
        {
            _portfolio.SetCash(10000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _algorithm.SetFinishedWarmingUp();

            var getMaximumOrderQuantityForTargetValueResult = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 10m);
            // Quantity * 15000m + fees + price buffer <= 100000 USD Target
            Assert.AreEqual(6.64672648m, getMaximumOrderQuantityForTargetValueResult.Quantity);

            var order = new MarketOrder(_btcusd.Symbol, getMaximumOrderQuantityForTargetValueResult.Quantity, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void CalculatesMaximumOrderQuantityCorrectlyForAlmostNoCashRemaining()
        {
            _portfolio.SetCash(0.00000000001m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _algorithm.SetFinishedWarmingUp();

            var getMaximumOrderQuantityForTargetValueResult = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 10000 / _portfolio.TotalPortfolioValue);
            // We don't have enough cash, but GetMaximumOrderQuantityForTargetValue does not care about this :)
            Assert.AreEqual(0.66467264m, getMaximumOrderQuantityForTargetValueResult.Quantity);

            var order = new MarketOrder(_btcusd.Symbol, getMaximumOrderQuantityForTargetValueResult.Quantity, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void CalculatesMaximumOrderQuantityCorrectlyForNoOrderFee()
        {
            _portfolio.SetCash(100000m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _algorithm.Securities[_btcusd.Symbol].SetFeeModel(new ConstantFeeModel(0));

            _btcusd.SetMarketPrice(new Tick { Value = 15000m });

            _algorithm.SetFinishedWarmingUp();
            var getMaximumOrderQuantityForTargetValueResult  = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 0.1m);
            // Quantity * 15000m + fees (0) + price buffer <= 10000 USD Target
            Assert.AreEqual(0.66666666m, getMaximumOrderQuantityForTargetValueResult.Quantity);

            var order = new MarketOrder(_btcusd.Symbol, getMaximumOrderQuantityForTargetValueResult.Quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void MarketBuyOrderChecksExistingHoldings()
        {
            _portfolio.SetCash(8000);
            _portfolio.CashBook.Add("BTC", 0.2m, 10000m);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 10000m });
            _algorithm.SetFinishedWarmingUp();

            Assert.AreEqual(10000m, _portfolio.TotalPortfolioValue);

            // Maximum we can market buy for (10000-2000) = 8000 USD is 0.79760717m BTC
            var quantity = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 10000m / _portfolio.TotalPortfolioValue).Quantity;
            Assert.AreEqual(0.79760717m, quantity);

            // the maximum order quantity can be executed
            var order = new MarketOrder(_btcusd.Symbol, quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void MarketBuyBtcWithEurCalculatesBuyingPowerProperlyWithExistingHoldings()
        {
            _portfolio.SetCash("EUR", 20000m, 1.20m);
            _portfolio.CashBook.Add("BTC", 1m, 12000m);

            _btceur.SetMarketPrice(new Tick { Value = 10000m });

            // Maximum we can market buy with 20000 EUR is 1.99401794m BTC
            // target value = 30000 EUR = 20000 EUR in cash + 10000 EUR in BTC
            var targetValue = 30000m * _portfolio.CashBook["EUR"].ConversionRate / _portfolio.TotalPortfolioValue;
            Assert.AreEqual(1.99401794m, _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btceur, targetValue).Quantity);

            // Available cash = 20000 EUR, can buy 1.994 BTC at 10000 (plus fees)
            var order = new MarketOrder(_btceur.Symbol, 1.99401794m, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);

            // Available cash = 20000 EUR, cannot buy 2 BTC at 10000 (plus fees)
            order = new MarketOrder(_btceur.Symbol, 2m, DateTime.UtcNow);
            Assert.IsFalse(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btceur, order).IsSufficient);
        }

        [Test]
        public void MarketBuyOrderUsesAskPriceIfAvailable()
        {
            _portfolio.SetCash(10000);

            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _btcusd.SetMarketPrice(new Tick { Value = 10000m, BidPrice = 9950, AskPrice = 10050, TickType = TickType.Quote });
            _algorithm.SetFinishedWarmingUp();

            Assert.AreEqual(10000m, _portfolio.TotalPortfolioValue);

            // Maximum we can market buy at ask price with 10000 USD is 0.99204872m BTC
            var quantity = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_portfolio, _btcusd, 1m).Quantity;
            Assert.AreEqual(0.99204872m, quantity);

            // the maximum order quantity can be executed
            var order = new MarketOrder(_btcusd.Symbol, quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);
        }

        [Test]
        public void ZeroTargetWithZeroHoldingsIsNotAnError()
        {
            _btcusd = _algorithm.AddCrypto("BTCUSD");

            var result = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_algorithm.Portfolio, _btcusd, 0);

            var order = new MarketOrder(_btcusd.Symbol, result.Quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            Assert.AreEqual(0, result.Quantity);
            Assert.AreEqual(string.Empty, result.Reason);
            Assert.AreEqual(false, result.IsError);
        }

        [Test]
        public void ZeroTargetWithNonZeroHoldingsReturnsNegativeOfQuantity()
        {
            _btcusd = _algorithm.AddCrypto("BTCUSD");
            _portfolio.CashBook.Add("BTC", 1m, 12000m);

            var result = _buyingPowerModel.GetMaximumOrderQuantityForTargetValue(_algorithm.Portfolio, _btcusd, 0);

            var order = new MarketOrder(_btcusd.Symbol, result.Quantity, DateTime.UtcNow);
            Assert.IsTrue(_buyingPowerModel.HasSufficientBuyingPowerForOrder(_portfolio, _btcusd, order).IsSufficient);

            Assert.AreEqual(-1, result.Quantity);
            Assert.AreEqual(string.Empty, result.Reason);
            Assert.AreEqual(false, result.IsError);
        }

        private void SubmitLimitOrder(Symbol symbol, decimal quantity, decimal limitPrice)
        {
            using (var resetEvent = new ManualResetEvent(false))
            {
                EventHandler<OrderEvent> handler = (s, e) => { resetEvent.Set(); };

                _brokerage.OrderStatusChanged += handler;

                _algorithm.LimitOrder(symbol, quantity, limitPrice);

                resetEvent.WaitOne();

                _brokerage.OrderStatusChanged -= handler;
            }
        }

        private void SubmitStopMarketOrder(Symbol symbol, decimal quantity, decimal stopPrice)
        {
            using (var resetEvent = new ManualResetEvent(false))
            {
                EventHandler<OrderEvent> handler = (s, e) => { resetEvent.Set(); };

                _brokerage.OrderStatusChanged += handler;

                _algorithm.StopMarketOrder(symbol, quantity, stopPrice);

                resetEvent.WaitOne();

                _brokerage.OrderStatusChanged -= handler;
            }
        }
    }
}
