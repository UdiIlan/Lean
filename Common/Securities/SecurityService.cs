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
 *
*/

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities
{
    /// <summary>
    /// This class implements interface <see cref="ISecurityService"/> providing methods for creating new <see cref="Security"/>
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly CashBook _cashBook;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;
        private readonly ISecurityInitializerProvider _securityInitializerProvider;
        private bool _isLiveMode;

        /// <summary>
        /// Creates a new instance of the SecurityService class
        /// </summary>
        public SecurityService(CashBook cashBook,
            MarketHoursDatabase marketHoursDatabase,
            SymbolPropertiesDatabase symbolPropertiesDatabase,
            ISecurityInitializerProvider securityInitializerProvider)
        {
            _cashBook = cashBook;
            _marketHoursDatabase = marketHoursDatabase;
            _symbolPropertiesDatabase = symbolPropertiesDatabase;
            _securityInitializerProvider = securityInitializerProvider;
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateSecurity(Symbol symbol,
            List<SubscriptionDataConfig> subscriptionDataConfigList,
            decimal leverage = 0,
            bool addToSymbolCache = true)
        {
            var configList = new SubscriptionDataConfigList(symbol);
            configList.AddRange(subscriptionDataConfigList);

            var exchangeHours = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType).ExchangeHours;

            var defaultQuoteCurrency = CashBook.AccountCurrency;
            if (symbol.ID.SecurityType == SecurityType.Forex || symbol.ID.SecurityType == SecurityType.Crypto)
            {
                defaultQuoteCurrency = symbol.Value.Substring(3);
            }

            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.ID.SecurityType, defaultQuoteCurrency);

            // add the symbol to our cache
            if (addToSymbolCache)
            {
                SymbolCache.Set(symbol.Value, symbol);
            }

            // verify the cash book is in a ready state
            var quoteCurrency = symbolProperties.QuoteCurrency;
            if (!_cashBook.ContainsKey(quoteCurrency))
            {
                // since we have none it's safe to say the conversion is zero
                _cashBook.Add(quoteCurrency, 0, 0);
            }
            if (symbol.ID.SecurityType == SecurityType.Forex || symbol.ID.SecurityType == SecurityType.Crypto)
            {
                // decompose the symbol into each currency pair
                string baseCurrency;
                Forex.Forex.DecomposeCurrencyPair(symbol.Value, out baseCurrency, out quoteCurrency);

                if (!_cashBook.ContainsKey(baseCurrency))
                {
                    // since we have none it's safe to say the conversion is zero
                    _cashBook.Add(baseCurrency, 0, 0);
                }
                if (!_cashBook.ContainsKey(quoteCurrency))
                {
                    // since we have none it's safe to say the conversion is zero
                    _cashBook.Add(quoteCurrency, 0, 0);
                }
            }

            var quoteCash = _cashBook[symbolProperties.QuoteCurrency];

            Security security;
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Equity:
                    security = new Equity.Equity(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook);
                    break;

                case SecurityType.Option:
                    if (addToSymbolCache) SymbolCache.Set(symbol.Underlying.Value, symbol.Underlying);
                    security = new Option.Option(symbol, exchangeHours, _cashBook[CashBook.AccountCurrency], new Option.OptionSymbolProperties(symbolProperties), _cashBook);
                    break;

                case SecurityType.Future:
                    security = new Future.Future(symbol, exchangeHours, _cashBook[CashBook.AccountCurrency], symbolProperties, _cashBook);
                    break;

                case SecurityType.Forex:
                    security = new Forex.Forex(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook);
                    break;

                case SecurityType.Cfd:
                    security = new Cfd.Cfd(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook);
                    break;

                case SecurityType.Crypto:
                    security = new Crypto.Crypto(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook);
                    break;

                default:
                case SecurityType.Base:
                    security = new Security(symbol, exchangeHours, quoteCash, symbolProperties, _cashBook);
                    break;
            }

            // if we're just creating this security and it only has an internal
            // feed, mark it as non-tradable since the user didn't request this data
            if (!configList.IsInternalFeed)
            {
                security.IsTradable = true;
            }

            security.AddData(configList);

            // invoke the security initializer
            _securityInitializerProvider.SecurityInitializer.Initialize(security);

            // if leverage was specified then apply to security after the initializer has run, parameters of this
            // method take precedence over the intializer
            if (leverage > 0)
            {
                security.SetLeverage(leverage);
            }

            // In live mode, equity assumes specific price variation model
            if (_isLiveMode && security.Type == SecurityType.Equity)
            {
                security.PriceVariationModel = new EquityPriceVariationModel();
            }

            return security;
        }

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        public Security CreateSecurity(Symbol symbol, SubscriptionDataConfig subscriptionDataConfig, decimal leverage = 0, bool addToSymbolCache = true)
        {
            return CreateSecurity(symbol, new List<SubscriptionDataConfig> { subscriptionDataConfig }, leverage, addToSymbolCache);
        }

        /// <summary>
        /// Set live mode state of the algorithm
        /// </summary>
        /// <param name="isLiveMode">True, live mode is enabled</param>
        public void SetLiveMode(bool isLiveMode)
        {
            _isLiveMode = isLiveMode;
        }
    }
}
