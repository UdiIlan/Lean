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
using QuantConnect.Data;
using QuantConnect.Packets;
using Quobject.SocketIoClientDotNet.Client;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using QuantConnect.Interfaces;
using NodaTime;
using System.Globalization;

namespace QuantConnect.ToolBox.IEX
{
    /// <summary>
    /// IEX live data handler.
    /// Data provided for free by IEX. See more at https://iextrading.com/api-exhibit-a
    /// </summary>
    public class IEXDataQueueHandler : HistoryProviderBase, IDataQueueHandler, IDisposable
    {
        // using SocketIoClientDotNet is a temp solution until IEX implements standard WebSockets protocol
        private Socket _socket;

        private ConcurrentDictionary<string, Symbol> _symbols = new ConcurrentDictionary<string, Symbol>(StringComparer.InvariantCultureIgnoreCase);
        private Manager _manager;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Unspecified);
        private TaskCompletionSource<bool> _connected = new TaskCompletionSource<bool>();
        private Task _lastEmitTask;
        private bool _subscribedToAll;
        private int _dataPointCount;

        private BlockingCollection<BaseData> _outputCollection = new BlockingCollection<BaseData>();

        public string Endpoint { get; internal set; }

        public bool IsConnected
        {
            get { return _manager.ReadyState == Manager.ReadyStateEnum.OPEN; }
        }

        public IEXDataQueueHandler(bool live = true)
        {
            Endpoint = "https://ws-api.iextrading.com/1.0/tops";
            if (live)
                Reconnect();
        }

        internal void Reconnect()
        {
            try
            {
                _socket = IO.Socket(Endpoint,
                    new IO.Options()
                    {
                        // default is 1000, default attempts is int.MaxValue
                        ReconnectionDelay = 1000
                    });
                _socket.On(Socket.EVENT_CONNECT, () =>
                {
                    _connected.TrySetResult(true);
                    Log.Trace("IEXDataQueueHandler.Reconnect(): Connected to IEX live data");
                    Log.Trace("IEXDataQueueHandler.Reconnect(): IEX Real-Time Price");
                });

                _socket.On("message", message => ProcessJsonObject(JObject.Parse((string)message)));
                _manager = _socket.Io();
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.Reconnect(): " + err.Message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProcessJsonObject(JObject message)
        {
            try
            {
                // https://iextrading.com/developer/#tops-tops-response
                var symbolString = message["symbol"].Value<string>();
                Symbol symbol;
                if (!_symbols.TryGetValue(symbolString, out symbol))
                {
                    if (_subscribedToAll)
                    {
                        symbol = Symbol.Create(symbolString, SecurityType.Equity, Market.USA);
                    }
                    else
                    {
                        Log.Trace("IEXDataQueueHandler.ProcessJsonObject(): Received unexpected symbol '" + symbolString + "' from IEX in IEXDataQueueHandler");
                        return;
                    }
                }
                var bidSize = message["bidSize"].Value<long>();
                var bidPrice = message["bidPrice"].Value<decimal>();
                var askSize = message["askSize"].Value<long>();
                var askPrice = message["askPrice"].Value<decimal>();
                var volume = message["volume"].Value<int>();
                var lastSalePrice = message["lastSalePrice"].Value<decimal>();
                var lastSaleSize = message["lastSaleSize"].Value<int>();
                var lastSaleTime = message["lastSaleTime"].Value<long>();
                var lastSaleDateTime = UnixEpoch.AddMilliseconds(lastSaleTime);
                var lastUpdated = message["lastUpdated"].Value<long>();
                if (lastUpdated == -1)
                {
                    // there were no trades on this day
                    return;
                }
                var lastUpdatedDatetime = UnixEpoch.AddMilliseconds(lastUpdated);

                var tick = new Tick()
                {
                    Symbol = symbol,
                    Time = lastUpdatedDatetime.ConvertFromUtc(TimeZones.NewYork),
                    TickType = lastUpdatedDatetime == lastSaleDateTime ? TickType.Trade : TickType.Quote,
                    Exchange = "IEX",
                    BidSize = bidSize,
                    BidPrice = bidPrice,
                    AskSize = askSize,
                    AskPrice = askPrice,
                    Value = lastSalePrice,
                    Quantity = lastSaleSize
                };
                _outputCollection.TryAdd(tick);
            }
            catch (Exception err)
            {
                // this method should never fail
                Log.Error("IEXDataQueueHandler.ProcessJsonObject(): " + err.Message);
            }
        }

        /// <summary>
        /// Desktop/Local doesn't support live data from this handler
        /// </summary>
        /// <returns>Tick</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            return _outputCollection.GetConsumingEnumerable();
        }

        /// <summary>
        /// Subscribe to symbols
        /// </summary>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var symbol in symbols)
                {
                    // IEX only supports equities
                    if (symbol.SecurityType != SecurityType.Equity) continue;
                    if (symbol.Value.Equals("firehose", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _subscribedToAll = true;
                    }
                    if (_symbols.TryAdd(symbol.Value, symbol))
                    {
                        // added new symbol
                        sb.Append(symbol.Value);
                        sb.Append(",");
                    }
                }
                var symbolsList = sb.ToString().TrimEnd(',');
                if (!String.IsNullOrEmpty(symbolsList))
                {
                    SocketSafeAsyncEmit("subscribe", symbolsList);
                }
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.Subscribe(): " + err.Message);
            }
        }


        /// <summary>
        /// Unsubscribe from symbols
        /// </summary>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var symbol in symbols)
                {
                    // IEX only supports equities
                    if (symbol.SecurityType != SecurityType.Equity) continue;
                    Symbol tmp;
                    if (_symbols.TryRemove(symbol.Value, out tmp))
                    {
                        // removed existing
                        Trace.Assert(symbol.Value == tmp.Value);
                        sb.Append(symbol.Value);
                        sb.Append(",");
                    }
                }
                var symbolsList = sb.ToString().TrimEnd(',');
                if (!String.IsNullOrEmpty(symbolsList))
                {
                    SocketSafeAsyncEmit("unsubscribe", symbolsList);
                }
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.Unsubscribe(): " + err.Message);
            }
        }

        /// <summary>
        /// This method is used to schedule _socket.Emit request until the connection state is OPEN
        /// </summary>
        /// <param name="symbol"></param>
        private void SocketSafeAsyncEmit(string command, string value)
        {
            Task.Run(async () =>
            {
                await _connected.Task;
                const int retriesLimit = 100;
                var retriesCount = 0;
                while (true)
                {
                    try
                    {
                        if (_manager.ReadyState == Manager.ReadyStateEnum.OPEN)
                        {
                            // there is an ACK functionality in socket.io, but IEX will be moving to standard WebSockets
                            // and this retry logic is just for rare cases of connection interrupts
                            _socket.Emit(command, value);
                            break;
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error("IEXDataQueueHandler.SocketSafeAsyncEmit(): " + err.Message);
                    }
                    await Task.Delay(100);
                    retriesCount++;
                    if (retriesCount >= retriesLimit)
                    {
                        Log.Error("IEXDataQueueHandler.SocketSafeAsyncEmit(): " +
                                  (new TimeoutException("Cannot subscribe to symbol :" + value)));
                        break;
                    }
                }
            }, _cts.Token)
            .ContinueWith((t) =>
            {
                Log.Error("IEXDataQueueHandler.SocketSafeAsyncEmit(): " + t.Exception.Message);
                return t;

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Dispose connection to IEX
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _outputCollection.CompleteAdding();
            _cts.Cancel();
            if (_socket != null)
            {
                _socket.Disconnect();
                _socket.Close();
            }
            Log.Trace("IEXDataQueueHandler.Dispose(): Disconnected from IEX live data");
        }

        ~IEXDataQueueHandler()
        {
            Dispose(false);
        }

        #region IHistoryProvider implementation

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _dataPointCount;

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            foreach (var request in requests)
            {
                foreach (var slice in ProcessHistoryRequests(request))
                {
                    yield return slice;
                }
            }
        }

        /// <summary>
        /// Populate request data
        /// </summary>
        private IEnumerable<Slice> ProcessHistoryRequests(Data.HistoryRequest request)
        {
            var ticker = request.Symbol.ID.Symbol;
            var start = request.StartTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            var end = request.EndTimeUtc.ConvertFromUtc(TimeZones.NewYork);

            if (request.Resolution == Resolution.Minute && start <= DateTime.Today.AddDays(-30))
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls with minute resolution for IEX available only for trailing 30 calendar days.");
                yield break;
            } else if (request.Resolution != Resolution.Daily && request.Resolution != Resolution.Minute)
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls for IEX only support daily & minute resolution.");
                yield break;
            }
            if (start <= DateTime.Today.AddYears(-5))
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls for IEX only support a maximum of 5 years history.");
                yield break;
            }

            Log.Trace(string.Format("IEXDataQueueHandler.ProcessHistoryRequests(): Submitting request: {0}-{1}: {2} {3}->{4}", request.Symbol.SecurityType, ticker, request.Resolution, start, end));

            var span = end.Date - start.Date;
            var suffixes = new List<string>();
            if (span.Days < 30 && request.Resolution == Resolution.Minute)
            {
                var begin = start;
                while (begin < end)
                {
                    suffixes.Add("date/" + begin.ToString("yyyyMMdd"));
                    begin = begin.AddDays(1);
                }
            }
            else if (span.Days < 30)
            {
                suffixes.Add("1m");
            }
            else if (span.Days < 3*30)
            {
                suffixes.Add("3m");
            }
            else if (span.Days < 6 * 30)
            {
                suffixes.Add("6m");
            }
            else if (span.Days < 12 * 30)
            {
                suffixes.Add("1y");
            }
            else if (span.Days < 24 * 30)
            {
                suffixes.Add("2y");
            }
            else
            {
                suffixes.Add("5y");
            }

            // Download and parse data
            var client = new System.Net.WebClient();
            foreach (var suffix in suffixes)
            {
                var response = client.DownloadString("https://api.iextrading.com/1.0/stock/" + ticker + "/chart/" + suffix);
                var parsedResponse = JArray.Parse(response);

                foreach (var item in parsedResponse.Children())
                {
                    DateTime date;
                    if (item["minute"] != null)
                    {
                        date = DateTime.ParseExact(item["date"].Value<string>(), "yyyyMMdd", CultureInfo.InvariantCulture);
                        var mins = TimeSpan.ParseExact(item["minute"].Value<string>(), "hh\\:mm", CultureInfo.InvariantCulture);
                        date += mins;
                    }
                    else
                    {
                        date = DateTime.Parse(item["date"].Value<string>());
                    }

                    if (date.Date < start.Date || date.Date > end.Date)
                    {
                        continue;
                    }

                    Interlocked.Increment(ref _dataPointCount);

                    if (item["open"] == null)
                    {
                        continue;
                    }
                    var open = item["open"].Value<decimal>();
                    var high = item["high"].Value<decimal>();
                    var low = item["low"].Value<decimal>();
                    var close = item["close"].Value<decimal>();
                    var volume = item["volume"].Value<int>();

                    TradeBar tradeBar = new TradeBar(date, request.Symbol, open, high, low, close, volume);

                    yield return new Slice(tradeBar.EndTime, new[] { tradeBar });
                }
            }
        }

        #endregion
    }
}
