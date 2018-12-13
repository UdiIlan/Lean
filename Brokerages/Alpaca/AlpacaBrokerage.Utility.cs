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
using NodaTime;
using QuantConnect.Brokerages.Alpaca.Markets;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using OrderStatus = QuantConnect.Orders.OrderStatus;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Alpaca Brokerage utility methods
    /// </summary>
    public partial class AlpacaBrokerage
    {
        /// <summary>
        /// Retrieves the current rate for each of a list of instruments
        /// </summary>
        /// <param name="instruments">the list of instruments to check</param>
        /// <returns>Dictionary containing the current quotes for each instrument</returns>
        private Dictionary<string, Tick> GetRates(IEnumerable<string> instruments)
        {
            CheckRateLimiting();

            var task = _restClient.ListQuotesAsync(instruments);
            var response = task.SynchronouslyAwaitTaskResult();

            return response
                .ToDictionary(
                    x => x.Symbol,
                    x => new Tick
                    {
                        Symbol = Symbol.Create(x.Symbol, SecurityType.Equity, Market.USA),
                        BidPrice = x.BidPrice,
                        AskPrice = x.AskPrice,
                        Time = x.LastTime,
                        TickType = TickType.Quote
                    }
                );
        }

        private IOrder GenerateAndPlaceOrder(Order order)
        {
            var quantity = (long)order.Quantity;
            var side = order.Quantity > 0 ? OrderSide.Buy : OrderSide.Sell;
            if (order.Quantity < 0) quantity = -quantity;
            Markets.OrderType type;
            decimal? limitPrice = null;
            decimal? stopPrice = null;
            var timeInForce = Markets.TimeInForce.Gtc;

            switch (order.Type)
            {
                case Orders.OrderType.Market:
                    type = Markets.OrderType.Market;
                    break;

                case Orders.OrderType.Limit:
                    type = Markets.OrderType.Limit;
                    limitPrice = ((LimitOrder)order).LimitPrice;
                    break;

                case Orders.OrderType.StopMarket:
                    type = Markets.OrderType.Stop;
                    stopPrice = ((StopMarketOrder)order).StopPrice;
                    break;

                case Orders.OrderType.StopLimit:
                    type = Markets.OrderType.StopLimit;
                    stopPrice = ((StopLimitOrder)order).StopPrice;
                    limitPrice = ((StopLimitOrder)order).LimitPrice;
                    break;

                case Orders.OrderType.MarketOnOpen:
                    type = Markets.OrderType.Market;
                    timeInForce = Markets.TimeInForce.Opg;
                    break;

                default:
                    throw new NotSupportedException("The order type " + order.Type + " is not supported.");
            }

            CheckRateLimiting();
            var task = _restClient.PostOrderAsync(order.Symbol.Value, quantity, side, type, timeInForce,
                limitPrice, stopPrice);

            var apOrder = task.SynchronouslyAwaitTaskResult();

            return apOrder;
        }

        /// <summary>
        /// Event handler for streaming events
        /// </summary>
        /// <param name="trade">The event object</param>
        private void OnTradeUpdate(ITradeUpdate trade)
        {
            Log.Trace($"AlpacaBrokerage.OnTradeUpdate(): Event:{trade.Event} OrderId:{trade.Order.OrderId} OrderStatus:{trade.Order.OrderStatus} FillQuantity: {trade.Order.FilledQuantity} Price: {trade.Price}");

            Order order;
            lock (_locker)
            {
                order = _orderProvider.GetOrderByBrokerageId(trade.Order.OrderId.ToString());
            }

            if (order != null)
            {
                if (trade.Event == TradeUpdateEvent.OrderFilled || trade.Event == TradeUpdateEvent.OrderPartiallyFilled)
                {
                    order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;

                    var status = trade.Event == TradeUpdateEvent.OrderFilled ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Fill Event")
                    {
                        Status = status,
                        FillPrice = trade.Price.Value,
                        FillQuantity = Convert.ToInt32(trade.Order.FilledQuantity) * (order.Direction == OrderDirection.Buy ? +1 : -1)
                    });
                }
                else if (trade.Event == TradeUpdateEvent.OrderCanceled)
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Alpaca Cancel Order Event") { Status = OrderStatus.Canceled });
                }
                else if (trade.Event == TradeUpdateEvent.OrderCancelRejected)
                {
                    var message = $"Order cancellation rejected: OrderId: {order.Id}";
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));
                }
            }
            else
            {
                Log.Error($"AlpacaBrokerage.OnTradeUpdate(): order id not found: {trade.Order.OrderId}");
            }
        }

        private static void OnNatsClientError(string error)
        {
            Log.Error($"NatsClient error: {error}");
        }

        private static void OnSockClientError(Exception exception)
        {
            Log.Error(exception, "SockClient error");
        }

        /// <summary>
        /// Downloads a list of TradeBars at the requested resolution
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="resolution">The requested resolution</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of bars</returns>
        private IEnumerable<TradeBar> DownloadTradeBars(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, Resolution resolution, DateTimeZone requestedTimeZone)
        {
            // Only minute/hour/daily resolutions supported
            if (resolution < Resolution.Minute)
            {
                yield break;
            }

            var period = resolution.ToTimeSpan();

            var startTime = startTimeUtc.RoundDown(period);
            var endTime = endTimeUtc.RoundDown(period).Add(period);

            while (startTime < endTime)
            {
                CheckRateLimiting();

                var task = resolution == Resolution.Daily
                    ? _restClient.ListDayAggregatesAsync(symbol.Value, startTime, endTime)
                    : _restClient.ListMinuteAggregatesAsync(symbol.Value, startTime, endTime);

                var time = startTime;
                var items = task.SynchronouslyAwaitTaskResult()
                    .Items
                    .Where(x => x.Time >= time)
                    .ToList();

                if (!items.Any())
                {
                    break;
                }

                if (resolution == Resolution.Hour)
                {
                    // aggregate minute tradebars into hourly tradebars
                    var bars = items
                        .GroupBy(x => x.Time.RoundDown(period))
                        .Select(
                            x => new TradeBar(
                                x.Key.ConvertFromUtc(requestedTimeZone),
                                symbol,
                                x.First().Open,
                                x.Max(t => t.High),
                                x.Min(t => t.Low),
                                x.Last().Close,
                                x.Sum(t => t.Volume),
                                period
                            ));

                    foreach (var bar in bars)
                    {
                        yield return bar;
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        // we do not convert time zones for daily bars here because the API endpoint
                        // for historical daily bars returns only dates instead of timestamps
                        yield return new TradeBar(
                            resolution == Resolution.Daily
                                ? item.Time
                                : item.Time.ConvertFromUtc(requestedTimeZone),
                            symbol,
                            item.Open,
                            item.High,
                            item.Low,
                            item.Close,
                            item.Volume,
                            period);
                    }
                }

                startTime = items.Last().Time.Add(period);
            }
        }

        /// <summary>
        /// Downloads a list of Trade ticks
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="startTimeUtc">The starting time (UTC)</param>
        /// <param name="endTimeUtc">The ending time (UTC)</param>
        /// <param name="requestedTimeZone">The requested timezone for the data</param>
        /// <returns>The list of ticks</returns>
        private IEnumerable<Tick> DownloadTradeTicks(Symbol symbol, DateTime startTimeUtc, DateTime endTimeUtc, DateTimeZone requestedTimeZone)
        {
            var startTime = startTimeUtc;

            var offset = 0L;
            while (startTime < endTimeUtc)
            {
                CheckRateLimiting();

                var date = startTime.ConvertFromUtc(requestedTimeZone).Date;

                var task = _restClient.ListHistoricalTradesAsync(symbol.Value, date, offset);

                var time = startTime;
                var items = task.SynchronouslyAwaitTaskResult()
                    .Items
                    .Where(x => DateTimeHelper.FromUnixTimeMilliseconds(x.TimeOffset) >= time)
                    .ToList();

                if (!items.Any())
                {
                    break;
                }

                foreach (var item in items)
                {
                    yield return new Tick
                    {
                        TickType = TickType.Trade,
                        Time = DateTimeHelper.FromUnixTimeMilliseconds(item.TimeOffset).ConvertFromUtc(requestedTimeZone),
                        Symbol = symbol,
                        Value = item.Price,
                        Quantity = item.Size
                    };
                }

                offset = items.Last().TimeOffset;
                startTime = DateTimeHelper.FromUnixTimeMilliseconds(offset);
            }
        }

        /// <summary>
        /// Aggregates a list of trade ticks into tradebars
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="ticks">The IEnumerable of ticks</param>
        /// <param name="period">The time span for the resolution</param>
        /// <returns></returns>
        internal static IEnumerable<TradeBar> AggregateTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan period)
        {
            return
                from t in ticks
                group t by t.Time.RoundDown(period)
                into g
                select new TradeBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Open = g.First().LastPrice,
                    High = g.Max(t => t.LastPrice),
                    Low = g.Min(t => t.LastPrice),
                    Close = g.Last().LastPrice,
                    Volume = g.Sum(t => t.Quantity),
                    Period = period
                };
        }

        /// <summary>
        /// Converts an Alpaca order into a LEAN order.
        /// </summary>
        private Order ConvertOrder(IOrder order)
        {
            var type = order.OrderType;

            Order qcOrder;
            switch (type)
            {
                case Markets.OrderType.Stop:
                    qcOrder = new StopMarketOrder
                    {
                        StopPrice = order.StopPrice.Value
                    };
                    break;

                case Markets.OrderType.Limit:
                    qcOrder = new LimitOrder
                    {
                        LimitPrice = order.LimitPrice.Value
                    };
                    break;

                case Markets.OrderType.StopLimit:
                    qcOrder = new StopLimitOrder
                    {
                        Price = order.StopPrice.Value,
                        LimitPrice = order.LimitPrice.Value
                    };
                    break;

                case Markets.OrderType.Market:
                    qcOrder = new MarketOrder();
                    break;

                default:
                    throw new NotSupportedException(
                        "An existing " + type + " working order was found and is currently unsupported. Please manually cancel the order before restarting the algorithm.");
            }

            var instrument = order.Symbol;
            var id = order.OrderId.ToString();

            qcOrder.Symbol = Symbol.Create(instrument, SecurityType.Equity, Market.USA);

            if (order.SubmittedAt != null)
            {
                qcOrder.Time = order.SubmittedAt.Value;
            }

            qcOrder.Quantity = order.OrderSide == OrderSide.Buy ? order.Quantity : -order.Quantity;
            qcOrder.Status = OrderStatus.None;
            qcOrder.BrokerId.Add(id);

            Order orderByBrokerageId;
            lock (_locker)
            {
                orderByBrokerageId = _orderProvider.GetOrderByBrokerageId(id);
            }

            if (orderByBrokerageId != null)
            {
                qcOrder.Id = orderByBrokerageId.Id;
            }

            if (order.ExpiredAt != null)
            {
                qcOrder.Properties.TimeInForce = Orders.TimeInForce.GoodTilDate(order.ExpiredAt.Value);
            }

            return qcOrder;
        }

        /// <summary>
        /// Converts an Alpaca position into a LEAN holding.
        /// </summary>
        private static Holding ConvertHolding(IPosition position)
        {
            const SecurityType securityType = SecurityType.Equity;
            var symbol = Symbol.Create(position.Symbol, securityType, Market.USA);

            return new Holding
            {
                Symbol = symbol,
                Type = securityType,
                AveragePrice = position.AverageEntryPrice,
                ConversionRate = 1.0m,
                CurrencySymbol = "$",
                Quantity = position.Side == PositionSide.Long ? position.Quantity : -position.Quantity
            };
        }

        private void CheckRateLimiting()
        {
            if (!_messagingRateLimiter.WaitToProceed(TimeSpan.Zero))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "RateLimit",
                    "The API request has been rate limited. To avoid this message, please reduce the frequency of API calls."));

                _messagingRateLimiter.WaitToProceed();
            }
        }
    }
}
