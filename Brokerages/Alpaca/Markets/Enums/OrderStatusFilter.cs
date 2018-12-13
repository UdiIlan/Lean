﻿/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Order statuses filter for <see cref="RestClient.ListOrdersAsync"/> call from Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatusFilter
    {
        /// <summary>
        /// Returns only open orders.
        /// </summary>
        [EnumMember(Value = "open")]
        Open,

        /// <summary>
        /// Returns only closed orders.
        /// </summary>
        [EnumMember(Value = "closed")]
        Closed,

        /// <summary>
        /// Returns all orders.
        /// </summary>
        [EnumMember(Value = "all")]
        All
    }
}