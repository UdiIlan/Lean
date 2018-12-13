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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Helper class used to create the corporate event providers
    /// <see cref="MappingEventProvider"/>, <see cref="SplitEventProvider"/>,
    /// <see cref="DividendEventProvider"/>, <see cref="DelistingEventProvider"/>
    /// </summary>
    public static class CorporateEventEnumeratorFactory
    {
        /// <summary>
        /// Creates a new <see cref="AuxiliaryDataEnumerator"/> that will hold the
        /// corporate event providers
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">Used for getting factor files</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="mapFileResolver">Used for resolving the correct map files</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        /// <returns>The new auxiliary data enumerator</returns>
        public static IEnumerator<BaseData> CreateEnumerators(
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            ITradableDatesNotifier tradableDayNotifier,
            MapFileResolver mapFileResolver,
            bool includeAuxiliaryData)
        {
            var mapFileToUse = GetMapFileToUse(config, mapFileResolver);
            var factorFile = GetFactorFileToUse(config, factorFileProvider);

            var enumerator = new AuxiliaryDataEnumerator(
                config,
                factorFile,
                mapFileToUse,
                new ITradableDateEventProvider[]
                {
                    new MappingEventProvider(),
                    new SplitEventProvider(),
                    new DividendEventProvider(),
                    new DelistingEventProvider()
                },
                tradableDayNotifier,
                includeAuxiliaryData);

            return enumerator;
        }

        private static MapFile GetMapFileToUse(
            SubscriptionDataConfig config,
            MapFileResolver mapFileResolver)
        {
            var mapFileToUse = new MapFile(config.Symbol.Value, new List<MapFileRow>());

            // load up the map and factor files for equities
            if (!config.IsCustomData && config.SecurityType == SecurityType.Equity)
            {
                try
                {
                    var mapFile = mapFileResolver.ResolveMapFile(
                        config.Symbol.ID.Symbol,
                        config.Symbol.ID.Date);

                    // only take the resolved map file if it has data, otherwise we'll use the empty one we defined above
                    if (mapFile.Any()) mapFileToUse = mapFile;
                }
                catch (Exception err)
                {
                    Log.Error(err, "CorporateEventEnumeratorFactory.GetMapFileToUse():" +
                        " Map File: " + config.Symbol.ID + ": ");
                }
            }

            // load up the map and factor files for underlying of equity option
            if (!config.IsCustomData && config.SecurityType == SecurityType.Option)
            {
                try
                {
                    var mapFile = mapFileResolver.ResolveMapFile(
                        config.Symbol.Underlying.ID.Symbol,
                        config.Symbol.Underlying.ID.Date);

                    // only take the resolved map file if it has data, otherwise we'll use the empty one we defined above
                    if (mapFile.Any()) mapFileToUse = mapFile;
                }
                catch (Exception err)
                {
                    Log.Error(err, "CorporateEventEnumeratorFactory.GetMapFileToUse():" +
                        " Map File: " + config.Symbol.ID + ": ");
                }
            }

            return mapFileToUse;
        }

        private static FactorFile GetFactorFileToUse(
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider)
        {
            var factorFileToUse = new FactorFile(config.Symbol.Value, new List<FactorFileRow>());

            if (!config.IsCustomData
                && config.SecurityType == SecurityType.Equity)
            {
                try
                {
                    var factorFile = factorFileProvider.Get(config.Symbol);
                    if (factorFile != null)
                    {
                        factorFileToUse = factorFile;
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "CorporateEventEnumeratorFactory.GetFactorFileToUse(): Factors File: "
                        + config.Symbol.ID + ": ");
                }
            }

            return factorFileToUse;
        }
    }
}
