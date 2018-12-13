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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class LiveTradingDataFeedTests
    {
        private static bool LogsEnabled = false; // this is for travis log no to fill up and reach the max size.
        private ManualTimeProvider _manualTimeProvider;
        private AlgorithmStub _algorithm;
        private Synchronizer _synchronizer;
        private readonly DateTime _startDate = new DateTime(2018, 08, 1, 11, 0, 0);
        private DataManager _dataManager;

        [SetUp]
        public void SetUp()
        {
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider = new ManualTimeProvider();
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            _algorithm = new AlgorithmStub(false);
        }

        [Test]
        public void EmitsData()
        {
            var feed = RunDataFeed(forex: new List<string> { Symbols.EURUSD });

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    emittedData = true;
                    var data = ts.Slice[Symbols.EURUSD];
                    ConsoleWriteLine("HasData: " + data);
                    ConsoleWriteLine();
                }
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void HandlesMultipleSecurities()
        {
            var equities = new List<string> { "SPY", "IBM", "AAPL", "GOOG", "MSFT", "BAC", "GS" };
            var forex = new List<string> { "EURUSD", "USDJPY", "GBPJPY", "AUDUSD", "NZDUSD" };

            var feed = RunDataFeed(equities: equities, forex: forex);

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                var delta = (DateTime.UtcNow - ts.Time).TotalMilliseconds;
                var values = ts.Slice.Keys.Select(x => x.Value).ToList();
                ConsoleWriteLine(((decimal)delta).SmartRounding() + "ms : " + string.Join(",", values));
                Assert.IsTrue(equities.All(x => values.Contains(x)));
                Assert.IsTrue(forex.All(x => values.Contains(x)));
                emittedData = true;
            });
            Assert.IsTrue(emittedData);
        }

        [Test]
        public void PerformanceBenchmark()
        {
            var symbolCount = 600;

            FuncDataQueueHandler queue;
            var count = new Count();
            var stopwatch = Stopwatch.StartNew();
            var feed = RunDataFeed(out queue, fdqh => ProduceBenchmarkTicks(fdqh, count), Resolution.Tick,
                equities: Enumerable.Range(0, symbolCount).Select(x => "E" + x.ToString()).ToList());

            var securitiesCount = _algorithm.Securities.Count;
            var expected = _algorithm.Securities.Keys.ToHashSet();
            Console.WriteLine("Securities.Count: " + securitiesCount);

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                ConsoleWriteLine("Count: " + ts.Slice.Keys.Count + " " + DateTime.UtcNow.ToString("o"));
                if (ts.Slice.Keys.Count != securitiesCount)
                {
                    var included = ts.Slice.Keys.ToHashSet();
                    expected.ExceptWith(included);
                    ConsoleWriteLine("Missing: " + string.Join(",", expected.OrderBy(x => x.Value)));
                }
            });
            stopwatch.Stop();

            Console.WriteLine("Total ticks: " + count.Value);
            Assert.GreaterOrEqual(count.Value, 700000);
            Console.WriteLine("Elapsed time: " + stopwatch.Elapsed);
            var ticksPerSec = count.Value / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Ticks/sec: " + ticksPerSec);
            Assert.GreaterOrEqual(ticksPerSec, 70000);
            var ticksPerSecPerSymbol = (count.Value / stopwatch.Elapsed.TotalSeconds) / symbolCount;
            Console.WriteLine("Ticks/sec/symbol: " + ticksPerSecPerSymbol);
            Assert.GreaterOrEqual(ticksPerSecPerSymbol, 100);
        }

        [Test]
        public void DoesNotSubscribeToCustomData()
        {
            // Current implementation only sends equity/forex subscriptions to the queue handler,
            // new impl sends all, the restriction shouldn't live in the feed, but rather in the
            // queue handler impl
            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(out dataQueueHandler, equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            _algorithm.AddData<CustomMockedFileBaseData>("CustomMockedFileBaseData");
            var customMockedFileBaseData = SymbolCache.GetSymbol("CustomMockedFileBaseData");

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), ts =>
            {
                ConsoleWriteLine("Count: " + ts.Slice.Keys.Count + " " + DateTime.UtcNow.ToString("o"));
                Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(customMockedFileBaseData));
                emittedData = true;
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void AddsSubscription_NewUserUniverse()
        {
            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(out dataQueueHandler, equities: new List<string> { "SPY" });

            var forexFxcmUserUniverse = UserDefinedUniverse.CreateSymbol(SecurityType.Forex, Market.FXCM);
            var emittedData = false;
            var newDataCount = 0;
            var securityChanges = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                securityChanges += ts.SecurityChanges.Count;
                if (!emittedData)
                {
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    if (ts.Data.Count > 0)
                    {
                        Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                    }
                    Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count);

                    _algorithm.AddSecurities(forex: new List<string> { "EURUSD" });
                    emittedData = true;
                }
                else
                {
                    if (dataQueueHandler.Subscriptions.Count == 2) // there could be some slices with no data
                    {
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                        if (ts.Data.Count > 0)
                        {
                            Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                        }
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD)
                                      || dataQueueHandler.Subscriptions.Contains(forexFxcmUserUniverse));
                        // Might delay a couple of Slices to send over the data, so we will count them
                        // and assert a minimum amount
                        if (ts.Slice.Keys.Contains(Symbols.EURUSD))
                        {
                            newDataCount++;
                        }
                    }
                    else
                    {
                        Assert.Fail($"Subscriptions.Count: {dataQueueHandler.Subscriptions.Count}");
                    }
                }
            });

            Console.WriteLine("newDataCount: " + newDataCount);
            Assert.AreEqual(2, securityChanges);

            Assert.GreaterOrEqual(newDataCount, 1000);
            Assert.IsTrue(emittedData);
        }

        [Test]
        public void AddsNewUniverse()
        {
            _algorithm.UniverseSettings.Resolution = Resolution.Second; // Default is Minute and we need something faster
            _algorithm.UniverseSettings.ExtendedMarketHours = true; // Current _startDate is at extended market hours

            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(out dataQueueHandler, forex: new List<string> { "EURUSD" });
            var firstTime = false;
            var securityChanges = 0;
            var newDataCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                securityChanges += ts.SecurityChanges.Count;
                if (!firstTime)
                {
                    Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count);
                    _algorithm.AddUniverse("TestUniverse", time => new List<string> { "AAPL", "SPY" });
                    firstTime = true;
                }
                else
                {
                    if (dataQueueHandler.Subscriptions.Count == 2)
                    {
                        Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count(x => x.Value.Contains("TESTUNIVERSE")));
                    }
                    else if(dataQueueHandler.Subscriptions.Count == 4)
                    {
                        Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count(x => x.Value.Contains("TESTUNIVERSE")));
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.AAPL));
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                        // Might delay a couple of Slices to send over the data, so we will count them and assert a minimum amount
                        if (ts.Slice.Keys.Contains(Symbols.AAPL)
                            && ts.Slice.Keys.Contains(Symbols.SPY))
                        {
                            newDataCount++;
                        }
                    }
                    else
                    {
                        Assert.Fail($"Subscriptions.Count: {dataQueueHandler.Subscriptions.Count}");
                    }
                }
            });

            Console.WriteLine("newDataCount: " + newDataCount);
            Assert.AreEqual(3, securityChanges);

            Assert.GreaterOrEqual(newDataCount, 490);
            Assert.IsTrue(firstTime);
        }

        [Test]
        public void AddsSubscription_SameUserUniverse()
        {
            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(out dataQueueHandler, equities: new List<string> { "SPY" });

            var emittedData = false;
            var newDataCount = 0;
            var securityChanges = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                securityChanges += ts.SecurityChanges.Count;
                if (!emittedData)
                {
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    if (ts.Data.Count > 0)
                    {
                        Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                    }
                    Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count);

                    _algorithm.AddSecurities(equities: new List<string> { "AAPL" });
                    emittedData = true;
                }
                else
                {
                    if (dataQueueHandler.Subscriptions.Count == 2) // there could be some slices with no data
                    {
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                        if (ts.Data.Count > 0)
                        {
                            Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                        }
                        Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.AAPL));
                        // Might delay a couple of Slices to send over the data, so we will count them
                        // and assert a minimum amount
                        if (ts.Slice.Keys.Contains(Symbols.AAPL))
                        {
                            newDataCount++;
                        }
                    }
                    else
                    {
                        Assert.Fail($"Subscriptions.Count: {dataQueueHandler.Subscriptions.Count}");
                    }
                }
            });

            Assert.GreaterOrEqual(newDataCount, 1000);
            Assert.IsTrue(emittedData);
            Assert.AreEqual(2, securityChanges + _algorithm.SecurityChangesRecord.Count);
            Assert.AreEqual(Symbols.AAPL, _algorithm.SecurityChangesRecord.First().AddedSecurities.First().Symbol);
        }

        [Test]
        public void Unsubscribes()
        {
            var customMockedFileBaseData = SymbolCache.GetSymbol("CustomMockedFileBaseData");
            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(out dataQueueHandler, equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            _algorithm.AddData<CustomMockedFileBaseData>("CustomMockedFileBaseData");

            var emittedData = false;
            var currentSubscriptionCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), false, ts =>
            {
                Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(customMockedFileBaseData));
                if (!emittedData)
                {
                    currentSubscriptionCount = dataQueueHandler.Subscriptions.Count;
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                    _dataManager.RemoveSubscription(_dataManager.DataFeedSubscriptions
                        .Single(sub => sub.Configuration.Symbol == Symbols.SPY).Configuration);
                    emittedData = true;
                }
                else
                {
                    Assert.AreEqual(currentSubscriptionCount - 1, dataQueueHandler.Subscriptions.Count);
                    Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                }
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void RemoveSecurity()
        {
            _algorithm.SetFinishedWarmingUp();
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var customMockedFileBaseData = SymbolCache.GetSymbol("CustomMockedFileBaseData");
            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(out dataQueueHandler, equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            _algorithm.AddData<CustomMockedFileBaseData>("CustomMockedFileBaseData");

            var emittedData = false;
            var currentSubscriptionCount = 0;
            var securityChanges = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                securityChanges += ts.SecurityChanges.Count;
                Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(customMockedFileBaseData));
                if (!emittedData)
                {
                    currentSubscriptionCount = dataQueueHandler.Subscriptions.Count;
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                    _algorithm.RemoveSecurity(Symbols.SPY);
                    emittedData = true;
                }
                else
                {
                    Assert.AreEqual(currentSubscriptionCount - 1, dataQueueHandler.Subscriptions.Count);
                    Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                }
            });

            Assert.IsTrue(emittedData);
            Assert.AreEqual(4, securityChanges + _algorithm.SecurityChangesRecord.Count);
            Assert.AreEqual(Symbols.SPY, _algorithm.SecurityChangesRecord.First().RemovedSecurities.First().Symbol);
        }

        [Test]
        public void BenchmarkTicksPerSecondWithTwentySymbols()
        {
            // this ran at ~25k ticks/per symbol for 20 symbols

            var feed = RunDataFeed(Resolution.Tick, equities: Enumerable.Range(0, 20).Select(x => x.ToString()).ToList());
            int ticks = 0;
            var averages = new List<decimal>();
            var timer = new Timer(state =>
            {
                var avg = ticks / 20m;
                Interlocked.Exchange(ref ticks, 0);
                Console.WriteLine("Average ticks per symbol: " + avg.SmartRounding());
                averages.Add(avg);
            }, null, Time.OneSecond, Time.OneSecond);

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), false, ts =>
            {
                Interlocked.Add(ref ticks, ts.Slice.Ticks.Sum(x => x.Value.Count));
            });

            timer.Dispose();
            var average = averages.Average();
            Console.WriteLine("\r\nAverage ticks per symbol per second: " + average);
            Assert.That(average, Is.GreaterThan(40));
        }

        [Test]
        public void EmitsForexDataWithRoundedUtcTimes()
        {
            var feed = RunDataFeed(forex: new List<string> { "EURUSD" });

            var emittedData = false;
            var lastTime = DateTime.UtcNow;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (!emittedData)
                {
                    emittedData = true;
                    lastTime = ts.Time;
                    return;
                }
                var delta = (DateTime.UtcNow - ts.Time).TotalMilliseconds;
                Assert.AreEqual(lastTime.Add(Time.OneSecond), ts.Time);
                Assert.AreEqual(1, ts.Slice.QuoteBars.Count);
                lastTime = ts.Time;
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void HandlesManyCustomDataSubscriptions()
        {
            var feed = RunDataFeed();
            for (int i = 0; i < 100; i++)
            {
                _algorithm.AddData<CustomMockedFileBaseData>((100 + i).ToString(), Resolution.Second, fillDataForward: false);
            }

            int count = 0;
            var emittedData = false;
            var stopwatch = Stopwatch.StartNew();

            var previousTime = DateTime.Now;
            Console.WriteLine("start: " + previousTime.ToString("o"));
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), false, ts =>
            {
                // because this is a remote file we may skip data points while the newest
                // version of the file is downloading [internet speed] and also we decide
                // not to emit old data
                stopwatch.Stop();
                if (ts.Slice.Count == 0) return;

                emittedData = true;
                count++;
                // make sure within 2 seconds
                var delta = DateTime.Now.Subtract(previousTime);
                previousTime = DateTime.Now;
                Assert.IsTrue(delta <= TimeSpan.FromSeconds(2), delta.ToString());
                ConsoleWriteLine("TimeProvider now: " + _manualTimeProvider.GetUtcNow() + " Count: "
                                  + ts.Slice.Count + ". Delta (ms): "
                                  + ((decimal)delta.TotalMilliseconds).SmartRounding() + Environment.NewLine);
            });

            Console.WriteLine("Count: " + count);
            Console.WriteLine("Spool up time: " + stopwatch.Elapsed);

            Assert.That(count, Is.GreaterThan(20));
            Assert.IsTrue(emittedData);
        }

        [Test, Ignore("These tests depend on a remote server")]
        public void HandlesRestApi()
        {
            var resolution = Resolution.Second;
            var symbol = SymbolCache.GetSymbol("RestApi");
            FuncDataQueueHandler dqgh;
            var feed = RunDataFeed(out dqgh);
            _algorithm.AddData<RestApiBaseData>("RestApi", resolution);

            var count = 0;
            var receivedData = false;
            var timeZone = _algorithm.Securities[symbol].Exchange.TimeZone;
            RestApiBaseData last = null;

            var cancellationTokenSource = new CancellationTokenSource();
            foreach (var ts in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!ts.Slice.ContainsKey(symbol)) return;

                count++;
                receivedData = true;
                var data = (RestApiBaseData)ts.Slice[symbol];
                var time = data.EndTime.ConvertToUtc(timeZone);
                ConsoleWriteLine(DateTime.UtcNow + ": Data time: " + time.ConvertFromUtc(TimeZones.NewYork) + Environment.NewLine);
                if (last != null)
                {
                    Assert.AreEqual(last.EndTime, data.EndTime.Subtract(resolution.ToTimeSpan()));
                }
                last = data;
            }

            feed.Exit();
            Assert.That(count, Is.GreaterThanOrEqualTo(8));
            Assert.IsTrue(receivedData);
            Assert.That(RestApiBaseData.ReaderCount, Is.LessThanOrEqualTo(30)); // we poll at 10x frequency

            Console.WriteLine("Count: " + count + " ReaderCount: " + RestApiBaseData.ReaderCount);
        }

        [Test]
        public void HandlesCoarseFundamentalData()
        {
            Symbol symbol = CoarseFundamental.CreateUniverseSymbol(Market.USA);
            _algorithm.AddUniverse(new FuncUniverse(
                new SubscriptionDataConfig(typeof(CoarseFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false),
                new UniverseSettings(Resolution.Second, 1, true, false, TimeSpan.Zero), SecurityInitializer.Null,
                coarse => coarse.Take(10).Select(x => x.Symbol)
                ));

            var lck = new object();
            BaseDataCollection list = null;
            const int coarseDataPointCount = 100000;
            var timer = new Timer(state =>
            {
                var currentTime = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
                Console.WriteLine(currentTime + ": timer.Elapsed");

                lock (state)
                {
                    list = new BaseDataCollection { Symbol = symbol };
                    list.Data.AddRange(Enumerable.Range(0, coarseDataPointCount).Select(x => new CoarseFundamental
                    {
                        Symbol = SymbolCache.GetSymbol(x.ToString()),
                        Time = currentTime - Time.OneDay, // hard-coded coarse period of one day
                    }));
                }
            }, lck, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(500));

            bool yieldedUniverseData = false;
            var feed = RunDataFeed(getNextTicksFunction : fdqh =>
            {
                lock (lck)
                {
                    if (list != null)
                        try
                        {
                            var tmp = list;
                            return new List<BaseData> { tmp };
                        }
                        finally
                        {
                            list = null;
                            yieldedUniverseData = true;
                        }
                }
                return Enumerable.Empty<BaseData>();
            });


            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                Assert.IsTrue(_dataManager.DataFeedSubscriptions
                    .Any(x => x.IsUniverseSelectionSubscription));
            });

            timer.Dispose();
            Assert.IsTrue(yieldedUniverseData);
        }


        [Test]
        public void FastExitsDoNotThrowUnhandledExceptions()
        {
            var algorithm = new AlgorithmStub();

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();

            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();

            var feed = new TestableLiveTradingDataFeed();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(
                algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                algorithm);
            algorithm.Securities.SetSecurityService(securityService);
            var dataManager = new DataManager(feed,
                new UniverseSelection(algorithm, securityService),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            var synchronizer = new TestableSynchronizer(_algorithm, dataManager, true);
            algorithm.AddSecurities(Resolution.Tick, Enumerable.Range(0, 20).Select(x => x.ToString()).ToList());
            var getNextTicksFunction = Enumerable.Range(0, 20).Select(x => new Tick { Symbol = SymbolCache.GetSymbol(x.ToString()) }).ToList();
            feed.DataQueueHandler = new FuncDataQueueHandler(handler => getNextTicksFunction);
            var mapFileProvider = new LocalDiskMapFileProvider();
            var fileProvider = new DefaultDataProvider();
            feed.Initialize(
                algorithm,
                job,
                resultHandler,
                mapFileProvider,
                new LocalDiskFactorFileProvider(mapFileProvider),
                fileProvider,
                dataManager,
                synchronizer);

            var unhandledExceptionWasThrown = false;
            try
            {
                feed.Exit();
            }
            catch (Exception ex)
            {
                QuantConnect.Logging.Log.Error(ex.ToString());
                unhandledExceptionWasThrown = true;
            }

            Thread.Sleep(500);
            Assert.IsFalse(unhandledExceptionWasThrown);
        }

        private IDataFeed RunDataFeed(Resolution resolution = Resolution.Second,
                                    List<string> equities = null,
                                    List<string> forex = null,
                                    Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null)
        {
            FuncDataQueueHandler dataQueueHandler;
            return RunDataFeed(out dataQueueHandler, getNextTicksFunction, resolution, equities, forex);
        }

        private IDataFeed RunDataFeed(out FuncDataQueueHandler dataQueueHandler, Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null,
            Resolution resolution = Resolution.Second, List<string> equities = null, List<string> forex = null)
        {
            _algorithm.SetStartDate(_startDate);

            var lastTime = _manualTimeProvider.GetUtcNow();
            getNextTicksFunction = getNextTicksFunction ?? (fdqh =>
            {
                var time = _manualTimeProvider.GetUtcNow();
                if (time == lastTime) return Enumerable.Empty<BaseData>();
                lastTime = time;
                return fdqh.Subscriptions.Where(symbol => !_algorithm.UniverseManager.ContainsKey(symbol)) // its not a universe
                    .Select(symbol => new Tick(lastTime.ConvertFromUtc(TimeZones.NewYork), symbol, 1, 2)
                    {
                        Quantity = 1,
                        // Symbol could not be in the Securities collections for the custom Universe tests. AlgorithmManager is in charge of adding them, and we are not executing that code here.
                        TickType = _algorithm.Securities.ContainsKey(symbol) ? _algorithm.Securities[symbol].SubscriptionDataConfig.TickType : TickType.Trade
                    });
            });

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();
            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();

            dataQueueHandler = new FuncDataQueueHandler(getNextTicksFunction);

            var feed = new TestableLiveTradingDataFeed(dataQueueHandler);
            var mapFileProvider = new LocalDiskMapFileProvider();
            var fileProvider = new DefaultDataProvider();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(_algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, _algorithm);
            _algorithm.Securities.SetSecurityService(securityService);
            _dataManager = new DataManager(feed,
                new UniverseSelection(_algorithm, securityService),
                _algorithm,
                _algorithm.TimeKeeper,
                marketHoursDatabase);
            _algorithm.SubscriptionManager.SetDataManager(_dataManager);
            _algorithm.AddSecurities(resolution, equities, forex);
            _synchronizer = new TestableSynchronizer(_algorithm, _dataManager, true, _manualTimeProvider);

            feed.Initialize(_algorithm, job, resultHandler, mapFileProvider,
                new LocalDiskFactorFileProvider(mapFileProvider), fileProvider, _dataManager, _synchronizer);

            _algorithm.PostInitialize();
            Thread.Sleep(150); // small handicap for the data to be pumped so TimeSlices have data of all subscriptions

            return feed;
        }

        private void ConsumeBridge(IDataFeed feed, TimeSpan timeout, Action<TimeSlice> handler)
        {
            ConsumeBridge(feed, timeout, false, handler);
        }

        private void ConsumeBridge(IDataFeed feed,
            TimeSpan timeout,
            bool alwaysInvoke,
            Action<TimeSlice> handler,
            bool noOutput = true)
        {
            var endTime = DateTime.UtcNow.Add(timeout);
            bool startedReceivingata = false;
            var cancellationTokenSource = new CancellationTokenSource();
            foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!noOutput)
                {
                    ConsoleWriteLine("\r\n" + $"Now (EDT): {DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork):o}" +
                                     $". TimeSlice.Time (EDT): {timeSlice.Time.ConvertFromUtc(TimeZones.NewYork):o}");
                }

                if (!startedReceivingata && timeSlice.Slice.Count != 0)
                {
                    startedReceivingata = true;
                }
                if (startedReceivingata || alwaysInvoke)
                {
                    handler(timeSlice);
                }
                _algorithm.OnEndOfTimeStep();
                _manualTimeProvider.AdvanceSeconds(1);
                if (endTime <= DateTime.UtcNow)
                {
                    feed.Exit();
                    cancellationTokenSource.Cancel();
                    return;
                }
            }
        }

        private class Count
        {
            public int Value;
        }

        private static IEnumerable<BaseData> ProduceBenchmarkTicks(FuncDataQueueHandler fdqh, Count count)
        {
            for (int i = 0; i < 10000; i++)
            {
                foreach (var symbol in fdqh.Subscriptions)
                {
                    count.Value++;
                    yield return new Tick { Symbol = symbol };
                }
            }
        }

        private void ConsoleWriteLine(string line = "")
        {
            if (LogsEnabled)
            {
                Console.WriteLine(line);
            }
        }
    }

    internal class TestableLiveTradingDataFeed : LiveTradingDataFeed
    {
        public IDataQueueHandler DataQueueHandler;

        public TestableLiveTradingDataFeed(IDataQueueHandler dataQueueHandler = null)
        {
            DataQueueHandler = dataQueueHandler;
        }

        protected override IDataQueueHandler GetDataQueueHandler()
        {
            return DataQueueHandler;
        }
    }

    internal class TestableSynchronizer : Synchronizer
    {
        private readonly ITimeProvider _timeProvider;
        public TestableSynchronizer(
            IAlgorithm algorithm,
            IDataFeedSubscriptionManager subscriptionManager,
            bool liveMode,
            ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? new RealTimeProvider();
            Initialize(algorithm,
                subscriptionManager,
                liveMode);
        }

        protected override ITimeProvider GetTimeProvider()
        {
            return _timeProvider;
        }
    }
}
