/* 
 * OANDA v20 REST API
 *
 * The full OANDA v20 REST API Specification. This specification defines how to interact with v20 Accounts, Trades, Orders, Pricing and more.
 *
 * OpenAPI spec version: 3.0.15
 * Contact: api@oanda.com
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace Oanda.RestV20.Model
{
    /// <summary>
    /// A MarketIfTouchedOrder is an order that is created with a price threshold, and will only be filled by a market price that is touches or crosses the threshold.
    /// </summary>
    [DataContract]
    public partial class MarketIfTouchedOrder :  IEquatable<MarketIfTouchedOrder>, IValidatableObject
    {
        /// <summary>
        /// The current state of the Order.
        /// </summary>
        /// <value>The current state of the Order.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StateEnum
        {
            
            /// <summary>
            /// Enum PENDING for "PENDING"
            /// </summary>
            [EnumMember(Value = "PENDING")]
            PENDING,
            
            /// <summary>
            /// Enum FILLED for "FILLED"
            /// </summary>
            [EnumMember(Value = "FILLED")]
            FILLED,
            
            /// <summary>
            /// Enum TRIGGERED for "TRIGGERED"
            /// </summary>
            [EnumMember(Value = "TRIGGERED")]
            TRIGGERED,
            
            /// <summary>
            /// Enum CANCELLED for "CANCELLED"
            /// </summary>
            [EnumMember(Value = "CANCELLED")]
            CANCELLED
        }

        /// <summary>
        /// The type of the Order. Always set to \"MARKET_IF_TOUCHED\" for Market If Touched Orders.
        /// </summary>
        /// <value>The type of the Order. Always set to \"MARKET_IF_TOUCHED\" for Market If Touched Orders.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            
            /// <summary>
            /// Enum MARKET for "MARKET"
            /// </summary>
            [EnumMember(Value = "MARKET")]
            MARKET,
            
            /// <summary>
            /// Enum LIMIT for "LIMIT"
            /// </summary>
            [EnumMember(Value = "LIMIT")]
            LIMIT,
            
            /// <summary>
            /// Enum STOP for "STOP"
            /// </summary>
            [EnumMember(Value = "STOP")]
            STOP,
            
            /// <summary>
            /// Enum MARKETIFTOUCHED for "MARKET_IF_TOUCHED"
            /// </summary>
            [EnumMember(Value = "MARKET_IF_TOUCHED")]
            MARKETIFTOUCHED,
            
            /// <summary>
            /// Enum TAKEPROFIT for "TAKE_PROFIT"
            /// </summary>
            [EnumMember(Value = "TAKE_PROFIT")]
            TAKEPROFIT,
            
            /// <summary>
            /// Enum STOPLOSS for "STOP_LOSS"
            /// </summary>
            [EnumMember(Value = "STOP_LOSS")]
            STOPLOSS,
            
            /// <summary>
            /// Enum TRAILINGSTOPLOSS for "TRAILING_STOP_LOSS"
            /// </summary>
            [EnumMember(Value = "TRAILING_STOP_LOSS")]
            TRAILINGSTOPLOSS
        }

        /// <summary>
        /// The time-in-force requested for the MarketIfTouched Order. Restricted to \"GTC\", \"GFD\" and \"GTD\" for MarketIfTouched Orders.
        /// </summary>
        /// <value>The time-in-force requested for the MarketIfTouched Order. Restricted to \"GTC\", \"GFD\" and \"GTD\" for MarketIfTouched Orders.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TimeInForceEnum
        {
            
            /// <summary>
            /// Enum GTC for "GTC"
            /// </summary>
            [EnumMember(Value = "GTC")]
            GTC,
            
            /// <summary>
            /// Enum GTD for "GTD"
            /// </summary>
            [EnumMember(Value = "GTD")]
            GTD,
            
            /// <summary>
            /// Enum GFD for "GFD"
            /// </summary>
            [EnumMember(Value = "GFD")]
            GFD,
            
            /// <summary>
            /// Enum FOK for "FOK"
            /// </summary>
            [EnumMember(Value = "FOK")]
            FOK,
            
            /// <summary>
            /// Enum IOC for "IOC"
            /// </summary>
            [EnumMember(Value = "IOC")]
            IOC
        }

        /// <summary>
        /// Specification of how Positions in the Account are modified when the Order is filled.
        /// </summary>
        /// <value>Specification of how Positions in the Account are modified when the Order is filled.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum PositionFillEnum
        {
            
            /// <summary>
            /// Enum OPENONLY for "OPEN_ONLY"
            /// </summary>
            [EnumMember(Value = "OPEN_ONLY")]
            OPENONLY,
            
            /// <summary>
            /// Enum REDUCEFIRST for "REDUCE_FIRST"
            /// </summary>
            [EnumMember(Value = "REDUCE_FIRST")]
            REDUCEFIRST,
            
            /// <summary>
            /// Enum REDUCEONLY for "REDUCE_ONLY"
            /// </summary>
            [EnumMember(Value = "REDUCE_ONLY")]
            REDUCEONLY,
            
            /// <summary>
            /// Enum DEFAULT for "DEFAULT"
            /// </summary>
            [EnumMember(Value = "DEFAULT")]
            DEFAULT,
            
            /// <summary>
            /// Enum POSITIONDEFAULT for "POSITION_DEFAULT"
            /// </summary>
            [EnumMember(Value = "POSITION_DEFAULT")]
            POSITIONDEFAULT
        }

        /// <summary>
        /// Specification of what component of a price should be used for comparison when determining if the Order should be filled.
        /// </summary>
        /// <value>Specification of what component of a price should be used for comparison when determining if the Order should be filled.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TriggerConditionEnum
        {
            
            /// <summary>
            /// Enum DEFAULT for "DEFAULT"
            /// </summary>
            [EnumMember(Value = "DEFAULT")]
            DEFAULT,
            
            /// <summary>
            /// Enum TRIGGERDEFAULT for "TRIGGER_DEFAULT"
            /// </summary>
            [EnumMember(Value = "TRIGGER_DEFAULT")]
            TRIGGERDEFAULT,
            
            /// <summary>
            /// Enum INVERSE for "INVERSE"
            /// </summary>
            [EnumMember(Value = "INVERSE")]
            INVERSE,
            
            /// <summary>
            /// Enum BID for "BID"
            /// </summary>
            [EnumMember(Value = "BID")]
            BID,
            
            /// <summary>
            /// Enum ASK for "ASK"
            /// </summary>
            [EnumMember(Value = "ASK")]
            ASK,
            
            /// <summary>
            /// Enum MID for "MID"
            /// </summary>
            [EnumMember(Value = "MID")]
            MID
        }

        /// <summary>
        /// The current state of the Order.
        /// </summary>
        /// <value>The current state of the Order.</value>
        [DataMember(Name="state", EmitDefaultValue=false)]
        public StateEnum? State { get; set; }
        /// <summary>
        /// The type of the Order. Always set to \"MARKET_IF_TOUCHED\" for Market If Touched Orders.
        /// </summary>
        /// <value>The type of the Order. Always set to \"MARKET_IF_TOUCHED\" for Market If Touched Orders.</value>
        [DataMember(Name="type", EmitDefaultValue=false)]
        public TypeEnum? Type { get; set; }
        /// <summary>
        /// The time-in-force requested for the MarketIfTouched Order. Restricted to \"GTC\", \"GFD\" and \"GTD\" for MarketIfTouched Orders.
        /// </summary>
        /// <value>The time-in-force requested for the MarketIfTouched Order. Restricted to \"GTC\", \"GFD\" and \"GTD\" for MarketIfTouched Orders.</value>
        [DataMember(Name="timeInForce", EmitDefaultValue=false)]
        public TimeInForceEnum? TimeInForce { get; set; }
        /// <summary>
        /// Specification of how Positions in the Account are modified when the Order is filled.
        /// </summary>
        /// <value>Specification of how Positions in the Account are modified when the Order is filled.</value>
        [DataMember(Name="positionFill", EmitDefaultValue=false)]
        public PositionFillEnum? PositionFill { get; set; }
        /// <summary>
        /// Specification of what component of a price should be used for comparison when determining if the Order should be filled.
        /// </summary>
        /// <value>Specification of what component of a price should be used for comparison when determining if the Order should be filled.</value>
        [DataMember(Name="triggerCondition", EmitDefaultValue=false)]
        public TriggerConditionEnum? TriggerCondition { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketIfTouchedOrder" /> class.
        /// </summary>
        /// <param name="Id">The Order&#39;s identifier, unique within the Order&#39;s Account..</param>
        /// <param name="CreateTime">The time when the Order was created..</param>
        /// <param name="State">The current state of the Order..</param>
        /// <param name="ClientExtensions">ClientExtensions.</param>
        /// <param name="Type">The type of the Order. Always set to \&quot;MARKET_IF_TOUCHED\&quot; for Market If Touched Orders..</param>
        /// <param name="Instrument">The MarketIfTouched Order&#39;s Instrument..</param>
        /// <param name="Units">The quantity requested to be filled by the MarketIfTouched Order. A posititive number of units results in a long Order, and a negative number of units results in a short Order..</param>
        /// <param name="Price">The price threshold specified for the MarketIfTouched Order. The MarketIfTouched Order will only be filled by a market price that crosses this price from the direction of the market price at the time when the Order was created (the initialMarketPrice). Depending on the value of the Order&#39;s price and initialMarketPrice, the MarketIfTouchedOrder will behave like a Limit or a Stop Order..</param>
        /// <param name="PriceBound">The worst market price that may be used to fill this MarketIfTouched Order..</param>
        /// <param name="TimeInForce">The time-in-force requested for the MarketIfTouched Order. Restricted to \&quot;GTC\&quot;, \&quot;GFD\&quot; and \&quot;GTD\&quot; for MarketIfTouched Orders..</param>
        /// <param name="GtdTime">The date/time when the MarketIfTouched Order will be cancelled if its timeInForce is \&quot;GTD\&quot;..</param>
        /// <param name="PositionFill">Specification of how Positions in the Account are modified when the Order is filled..</param>
        /// <param name="TriggerCondition">Specification of what component of a price should be used for comparison when determining if the Order should be filled..</param>
        /// <param name="InitialMarketPrice">The Market price at the time when the MarketIfTouched Order was created..</param>
        /// <param name="TakeProfitOnFill">TakeProfitOnFill.</param>
        /// <param name="StopLossOnFill">StopLossOnFill.</param>
        /// <param name="TrailingStopLossOnFill">TrailingStopLossOnFill.</param>
        /// <param name="TradeClientExtensions">TradeClientExtensions.</param>
        /// <param name="FillingTransactionID">ID of the Transaction that filled this Order (only provided when the Order&#39;s state is FILLED).</param>
        /// <param name="FilledTime">Date/time when the Order was filled (only provided when the Order&#39;s state is FILLED).</param>
        /// <param name="TradeOpenedID">Trade ID of Trade opened when the Order was filled (only provided when the Order&#39;s state is FILLED and a Trade was opened as a result of the fill).</param>
        /// <param name="TradeReducedID">Trade ID of Trade reduced when the Order was filled (only provided when the Order&#39;s state is FILLED and a Trade was reduced as a result of the fill).</param>
        /// <param name="TradeClosedIDs">Trade IDs of Trades closed when the Order was filled (only provided when the Order&#39;s state is FILLED and one or more Trades were closed as a result of the fill).</param>
        /// <param name="CancellingTransactionID">ID of the Transaction that cancelled the Order (only provided when the Order&#39;s state is CANCELLED).</param>
        /// <param name="CancelledTime">Date/time when the Order was cancelled (only provided when the state of the Order is CANCELLED).</param>
        /// <param name="ReplacesOrderID">The ID of the Order that was replaced by this Order (only provided if this Order was created as part of a cancel/replace)..</param>
        /// <param name="ReplacedByOrderID">The ID of the Order that replaced this Order (only provided if this Order was cancelled as part of a cancel/replace)..</param>
        public MarketIfTouchedOrder(string Id = default(string), string CreateTime = default(string), StateEnum? State = default(StateEnum?), ClientExtensions ClientExtensions = default(ClientExtensions), TypeEnum? Type = default(TypeEnum?), string Instrument = default(string), string Units = default(string), string Price = default(string), string PriceBound = default(string), TimeInForceEnum? TimeInForce = default(TimeInForceEnum?), string GtdTime = default(string), PositionFillEnum? PositionFill = default(PositionFillEnum?), TriggerConditionEnum? TriggerCondition = default(TriggerConditionEnum?), string InitialMarketPrice = default(string), TakeProfitDetails TakeProfitOnFill = default(TakeProfitDetails), StopLossDetails StopLossOnFill = default(StopLossDetails), TrailingStopLossDetails TrailingStopLossOnFill = default(TrailingStopLossDetails), ClientExtensions TradeClientExtensions = default(ClientExtensions), string FillingTransactionID = default(string), string FilledTime = default(string), string TradeOpenedID = default(string), string TradeReducedID = default(string), List<string> TradeClosedIDs = default(List<string>), string CancellingTransactionID = default(string), string CancelledTime = default(string), string ReplacesOrderID = default(string), string ReplacedByOrderID = default(string))
        {
            this.Id = Id;
            this.CreateTime = CreateTime;
            this.State = State;
            this.ClientExtensions = ClientExtensions;
            this.Type = Type;
            this.Instrument = Instrument;
            this.Units = Units;
            this.Price = Price;
            this.PriceBound = PriceBound;
            this.TimeInForce = TimeInForce;
            this.GtdTime = GtdTime;
            this.PositionFill = PositionFill;
            this.TriggerCondition = TriggerCondition;
            this.InitialMarketPrice = InitialMarketPrice;
            this.TakeProfitOnFill = TakeProfitOnFill;
            this.StopLossOnFill = StopLossOnFill;
            this.TrailingStopLossOnFill = TrailingStopLossOnFill;
            this.TradeClientExtensions = TradeClientExtensions;
            this.FillingTransactionID = FillingTransactionID;
            this.FilledTime = FilledTime;
            this.TradeOpenedID = TradeOpenedID;
            this.TradeReducedID = TradeReducedID;
            this.TradeClosedIDs = TradeClosedIDs;
            this.CancellingTransactionID = CancellingTransactionID;
            this.CancelledTime = CancelledTime;
            this.ReplacesOrderID = ReplacesOrderID;
            this.ReplacedByOrderID = ReplacedByOrderID;
        }
        
        /// <summary>
        /// The Order&#39;s identifier, unique within the Order&#39;s Account.
        /// </summary>
        /// <value>The Order&#39;s identifier, unique within the Order&#39;s Account.</value>
        [DataMember(Name="id", EmitDefaultValue=false)]
        public string Id { get; set; }
        /// <summary>
        /// The time when the Order was created.
        /// </summary>
        /// <value>The time when the Order was created.</value>
        [DataMember(Name="createTime", EmitDefaultValue=false)]
        public string CreateTime { get; set; }
        /// <summary>
        /// Gets or Sets ClientExtensions
        /// </summary>
        [DataMember(Name="clientExtensions", EmitDefaultValue=false)]
        public ClientExtensions ClientExtensions { get; set; }
        /// <summary>
        /// The MarketIfTouched Order&#39;s Instrument.
        /// </summary>
        /// <value>The MarketIfTouched Order&#39;s Instrument.</value>
        [DataMember(Name="instrument", EmitDefaultValue=false)]
        public string Instrument { get; set; }
        /// <summary>
        /// The quantity requested to be filled by the MarketIfTouched Order. A posititive number of units results in a long Order, and a negative number of units results in a short Order.
        /// </summary>
        /// <value>The quantity requested to be filled by the MarketIfTouched Order. A posititive number of units results in a long Order, and a negative number of units results in a short Order.</value>
        [DataMember(Name="units", EmitDefaultValue=false)]
        public string Units { get; set; }
        /// <summary>
        /// The price threshold specified for the MarketIfTouched Order. The MarketIfTouched Order will only be filled by a market price that crosses this price from the direction of the market price at the time when the Order was created (the initialMarketPrice). Depending on the value of the Order&#39;s price and initialMarketPrice, the MarketIfTouchedOrder will behave like a Limit or a Stop Order.
        /// </summary>
        /// <value>The price threshold specified for the MarketIfTouched Order. The MarketIfTouched Order will only be filled by a market price that crosses this price from the direction of the market price at the time when the Order was created (the initialMarketPrice). Depending on the value of the Order&#39;s price and initialMarketPrice, the MarketIfTouchedOrder will behave like a Limit or a Stop Order.</value>
        [DataMember(Name="price", EmitDefaultValue=false)]
        public string Price { get; set; }
        /// <summary>
        /// The worst market price that may be used to fill this MarketIfTouched Order.
        /// </summary>
        /// <value>The worst market price that may be used to fill this MarketIfTouched Order.</value>
        [DataMember(Name="priceBound", EmitDefaultValue=false)]
        public string PriceBound { get; set; }
        /// <summary>
        /// The date/time when the MarketIfTouched Order will be cancelled if its timeInForce is \&quot;GTD\&quot;.
        /// </summary>
        /// <value>The date/time when the MarketIfTouched Order will be cancelled if its timeInForce is \&quot;GTD\&quot;.</value>
        [DataMember(Name="gtdTime", EmitDefaultValue=false)]
        public string GtdTime { get; set; }
        /// <summary>
        /// The Market price at the time when the MarketIfTouched Order was created.
        /// </summary>
        /// <value>The Market price at the time when the MarketIfTouched Order was created.</value>
        [DataMember(Name="initialMarketPrice", EmitDefaultValue=false)]
        public string InitialMarketPrice { get; set; }
        /// <summary>
        /// Gets or Sets TakeProfitOnFill
        /// </summary>
        [DataMember(Name="takeProfitOnFill", EmitDefaultValue=false)]
        public TakeProfitDetails TakeProfitOnFill { get; set; }
        /// <summary>
        /// Gets or Sets StopLossOnFill
        /// </summary>
        [DataMember(Name="stopLossOnFill", EmitDefaultValue=false)]
        public StopLossDetails StopLossOnFill { get; set; }
        /// <summary>
        /// Gets or Sets TrailingStopLossOnFill
        /// </summary>
        [DataMember(Name="trailingStopLossOnFill", EmitDefaultValue=false)]
        public TrailingStopLossDetails TrailingStopLossOnFill { get; set; }
        /// <summary>
        /// Gets or Sets TradeClientExtensions
        /// </summary>
        [DataMember(Name="tradeClientExtensions", EmitDefaultValue=false)]
        public ClientExtensions TradeClientExtensions { get; set; }
        /// <summary>
        /// ID of the Transaction that filled this Order (only provided when the Order&#39;s state is FILLED)
        /// </summary>
        /// <value>ID of the Transaction that filled this Order (only provided when the Order&#39;s state is FILLED)</value>
        [DataMember(Name="fillingTransactionID", EmitDefaultValue=false)]
        public string FillingTransactionID { get; set; }
        /// <summary>
        /// Date/time when the Order was filled (only provided when the Order&#39;s state is FILLED)
        /// </summary>
        /// <value>Date/time when the Order was filled (only provided when the Order&#39;s state is FILLED)</value>
        [DataMember(Name="filledTime", EmitDefaultValue=false)]
        public string FilledTime { get; set; }
        /// <summary>
        /// Trade ID of Trade opened when the Order was filled (only provided when the Order&#39;s state is FILLED and a Trade was opened as a result of the fill)
        /// </summary>
        /// <value>Trade ID of Trade opened when the Order was filled (only provided when the Order&#39;s state is FILLED and a Trade was opened as a result of the fill)</value>
        [DataMember(Name="tradeOpenedID", EmitDefaultValue=false)]
        public string TradeOpenedID { get; set; }
        /// <summary>
        /// Trade ID of Trade reduced when the Order was filled (only provided when the Order&#39;s state is FILLED and a Trade was reduced as a result of the fill)
        /// </summary>
        /// <value>Trade ID of Trade reduced when the Order was filled (only provided when the Order&#39;s state is FILLED and a Trade was reduced as a result of the fill)</value>
        [DataMember(Name="tradeReducedID", EmitDefaultValue=false)]
        public string TradeReducedID { get; set; }
        /// <summary>
        /// Trade IDs of Trades closed when the Order was filled (only provided when the Order&#39;s state is FILLED and one or more Trades were closed as a result of the fill)
        /// </summary>
        /// <value>Trade IDs of Trades closed when the Order was filled (only provided when the Order&#39;s state is FILLED and one or more Trades were closed as a result of the fill)</value>
        [DataMember(Name="tradeClosedIDs", EmitDefaultValue=false)]
        public List<string> TradeClosedIDs { get; set; }
        /// <summary>
        /// ID of the Transaction that cancelled the Order (only provided when the Order&#39;s state is CANCELLED)
        /// </summary>
        /// <value>ID of the Transaction that cancelled the Order (only provided when the Order&#39;s state is CANCELLED)</value>
        [DataMember(Name="cancellingTransactionID", EmitDefaultValue=false)]
        public string CancellingTransactionID { get; set; }
        /// <summary>
        /// Date/time when the Order was cancelled (only provided when the state of the Order is CANCELLED)
        /// </summary>
        /// <value>Date/time when the Order was cancelled (only provided when the state of the Order is CANCELLED)</value>
        [DataMember(Name="cancelledTime", EmitDefaultValue=false)]
        public string CancelledTime { get; set; }
        /// <summary>
        /// The ID of the Order that was replaced by this Order (only provided if this Order was created as part of a cancel/replace).
        /// </summary>
        /// <value>The ID of the Order that was replaced by this Order (only provided if this Order was created as part of a cancel/replace).</value>
        [DataMember(Name="replacesOrderID", EmitDefaultValue=false)]
        public string ReplacesOrderID { get; set; }
        /// <summary>
        /// The ID of the Order that replaced this Order (only provided if this Order was cancelled as part of a cancel/replace).
        /// </summary>
        /// <value>The ID of the Order that replaced this Order (only provided if this Order was cancelled as part of a cancel/replace).</value>
        [DataMember(Name="replacedByOrderID", EmitDefaultValue=false)]
        public string ReplacedByOrderID { get; set; }
        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class MarketIfTouchedOrder {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  CreateTime: ").Append(CreateTime).Append("\n");
            sb.Append("  State: ").Append(State).Append("\n");
            sb.Append("  ClientExtensions: ").Append(ClientExtensions).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Instrument: ").Append(Instrument).Append("\n");
            sb.Append("  Units: ").Append(Units).Append("\n");
            sb.Append("  Price: ").Append(Price).Append("\n");
            sb.Append("  PriceBound: ").Append(PriceBound).Append("\n");
            sb.Append("  TimeInForce: ").Append(TimeInForce).Append("\n");
            sb.Append("  GtdTime: ").Append(GtdTime).Append("\n");
            sb.Append("  PositionFill: ").Append(PositionFill).Append("\n");
            sb.Append("  TriggerCondition: ").Append(TriggerCondition).Append("\n");
            sb.Append("  InitialMarketPrice: ").Append(InitialMarketPrice).Append("\n");
            sb.Append("  TakeProfitOnFill: ").Append(TakeProfitOnFill).Append("\n");
            sb.Append("  StopLossOnFill: ").Append(StopLossOnFill).Append("\n");
            sb.Append("  TrailingStopLossOnFill: ").Append(TrailingStopLossOnFill).Append("\n");
            sb.Append("  TradeClientExtensions: ").Append(TradeClientExtensions).Append("\n");
            sb.Append("  FillingTransactionID: ").Append(FillingTransactionID).Append("\n");
            sb.Append("  FilledTime: ").Append(FilledTime).Append("\n");
            sb.Append("  TradeOpenedID: ").Append(TradeOpenedID).Append("\n");
            sb.Append("  TradeReducedID: ").Append(TradeReducedID).Append("\n");
            sb.Append("  TradeClosedIDs: ").Append(TradeClosedIDs).Append("\n");
            sb.Append("  CancellingTransactionID: ").Append(CancellingTransactionID).Append("\n");
            sb.Append("  CancelledTime: ").Append(CancelledTime).Append("\n");
            sb.Append("  ReplacesOrderID: ").Append(ReplacesOrderID).Append("\n");
            sb.Append("  ReplacedByOrderID: ").Append(ReplacedByOrderID).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            return this.Equals(obj as MarketIfTouchedOrder);
        }

        /// <summary>
        /// Returns true if MarketIfTouchedOrder instances are equal
        /// </summary>
        /// <param name="other">Instance of MarketIfTouchedOrder to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(MarketIfTouchedOrder other)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            if (other == null)
                return false;

            return 
                (
                    this.Id == other.Id ||
                    this.Id != null &&
                    this.Id.Equals(other.Id)
                ) && 
                (
                    this.CreateTime == other.CreateTime ||
                    this.CreateTime != null &&
                    this.CreateTime.Equals(other.CreateTime)
                ) && 
                (
                    this.State == other.State ||
                    this.State != null &&
                    this.State.Equals(other.State)
                ) && 
                (
                    this.ClientExtensions == other.ClientExtensions ||
                    this.ClientExtensions != null &&
                    this.ClientExtensions.Equals(other.ClientExtensions)
                ) && 
                (
                    this.Type == other.Type ||
                    this.Type != null &&
                    this.Type.Equals(other.Type)
                ) && 
                (
                    this.Instrument == other.Instrument ||
                    this.Instrument != null &&
                    this.Instrument.Equals(other.Instrument)
                ) && 
                (
                    this.Units == other.Units ||
                    this.Units != null &&
                    this.Units.Equals(other.Units)
                ) && 
                (
                    this.Price == other.Price ||
                    this.Price != null &&
                    this.Price.Equals(other.Price)
                ) && 
                (
                    this.PriceBound == other.PriceBound ||
                    this.PriceBound != null &&
                    this.PriceBound.Equals(other.PriceBound)
                ) && 
                (
                    this.TimeInForce == other.TimeInForce ||
                    this.TimeInForce != null &&
                    this.TimeInForce.Equals(other.TimeInForce)
                ) && 
                (
                    this.GtdTime == other.GtdTime ||
                    this.GtdTime != null &&
                    this.GtdTime.Equals(other.GtdTime)
                ) && 
                (
                    this.PositionFill == other.PositionFill ||
                    this.PositionFill != null &&
                    this.PositionFill.Equals(other.PositionFill)
                ) && 
                (
                    this.TriggerCondition == other.TriggerCondition ||
                    this.TriggerCondition != null &&
                    this.TriggerCondition.Equals(other.TriggerCondition)
                ) && 
                (
                    this.InitialMarketPrice == other.InitialMarketPrice ||
                    this.InitialMarketPrice != null &&
                    this.InitialMarketPrice.Equals(other.InitialMarketPrice)
                ) && 
                (
                    this.TakeProfitOnFill == other.TakeProfitOnFill ||
                    this.TakeProfitOnFill != null &&
                    this.TakeProfitOnFill.Equals(other.TakeProfitOnFill)
                ) && 
                (
                    this.StopLossOnFill == other.StopLossOnFill ||
                    this.StopLossOnFill != null &&
                    this.StopLossOnFill.Equals(other.StopLossOnFill)
                ) && 
                (
                    this.TrailingStopLossOnFill == other.TrailingStopLossOnFill ||
                    this.TrailingStopLossOnFill != null &&
                    this.TrailingStopLossOnFill.Equals(other.TrailingStopLossOnFill)
                ) && 
                (
                    this.TradeClientExtensions == other.TradeClientExtensions ||
                    this.TradeClientExtensions != null &&
                    this.TradeClientExtensions.Equals(other.TradeClientExtensions)
                ) && 
                (
                    this.FillingTransactionID == other.FillingTransactionID ||
                    this.FillingTransactionID != null &&
                    this.FillingTransactionID.Equals(other.FillingTransactionID)
                ) && 
                (
                    this.FilledTime == other.FilledTime ||
                    this.FilledTime != null &&
                    this.FilledTime.Equals(other.FilledTime)
                ) && 
                (
                    this.TradeOpenedID == other.TradeOpenedID ||
                    this.TradeOpenedID != null &&
                    this.TradeOpenedID.Equals(other.TradeOpenedID)
                ) && 
                (
                    this.TradeReducedID == other.TradeReducedID ||
                    this.TradeReducedID != null &&
                    this.TradeReducedID.Equals(other.TradeReducedID)
                ) && 
                (
                    this.TradeClosedIDs == other.TradeClosedIDs ||
                    this.TradeClosedIDs != null &&
                    this.TradeClosedIDs.SequenceEqual(other.TradeClosedIDs)
                ) && 
                (
                    this.CancellingTransactionID == other.CancellingTransactionID ||
                    this.CancellingTransactionID != null &&
                    this.CancellingTransactionID.Equals(other.CancellingTransactionID)
                ) && 
                (
                    this.CancelledTime == other.CancelledTime ||
                    this.CancelledTime != null &&
                    this.CancelledTime.Equals(other.CancelledTime)
                ) && 
                (
                    this.ReplacesOrderID == other.ReplacesOrderID ||
                    this.ReplacesOrderID != null &&
                    this.ReplacesOrderID.Equals(other.ReplacesOrderID)
                ) && 
                (
                    this.ReplacedByOrderID == other.ReplacedByOrderID ||
                    this.ReplacedByOrderID != null &&
                    this.ReplacedByOrderID.Equals(other.ReplacedByOrderID)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                // Suitable nullity checks etc, of course :)
                if (this.Id != null)
                    hash = hash * 59 + this.Id.GetHashCode();
                if (this.CreateTime != null)
                    hash = hash * 59 + this.CreateTime.GetHashCode();
                if (this.State != null)
                    hash = hash * 59 + this.State.GetHashCode();
                if (this.ClientExtensions != null)
                    hash = hash * 59 + this.ClientExtensions.GetHashCode();
                if (this.Type != null)
                    hash = hash * 59 + this.Type.GetHashCode();
                if (this.Instrument != null)
                    hash = hash * 59 + this.Instrument.GetHashCode();
                if (this.Units != null)
                    hash = hash * 59 + this.Units.GetHashCode();
                if (this.Price != null)
                    hash = hash * 59 + this.Price.GetHashCode();
                if (this.PriceBound != null)
                    hash = hash * 59 + this.PriceBound.GetHashCode();
                if (this.TimeInForce != null)
                    hash = hash * 59 + this.TimeInForce.GetHashCode();
                if (this.GtdTime != null)
                    hash = hash * 59 + this.GtdTime.GetHashCode();
                if (this.PositionFill != null)
                    hash = hash * 59 + this.PositionFill.GetHashCode();
                if (this.TriggerCondition != null)
                    hash = hash * 59 + this.TriggerCondition.GetHashCode();
                if (this.InitialMarketPrice != null)
                    hash = hash * 59 + this.InitialMarketPrice.GetHashCode();
                if (this.TakeProfitOnFill != null)
                    hash = hash * 59 + this.TakeProfitOnFill.GetHashCode();
                if (this.StopLossOnFill != null)
                    hash = hash * 59 + this.StopLossOnFill.GetHashCode();
                if (this.TrailingStopLossOnFill != null)
                    hash = hash * 59 + this.TrailingStopLossOnFill.GetHashCode();
                if (this.TradeClientExtensions != null)
                    hash = hash * 59 + this.TradeClientExtensions.GetHashCode();
                if (this.FillingTransactionID != null)
                    hash = hash * 59 + this.FillingTransactionID.GetHashCode();
                if (this.FilledTime != null)
                    hash = hash * 59 + this.FilledTime.GetHashCode();
                if (this.TradeOpenedID != null)
                    hash = hash * 59 + this.TradeOpenedID.GetHashCode();
                if (this.TradeReducedID != null)
                    hash = hash * 59 + this.TradeReducedID.GetHashCode();
                if (this.TradeClosedIDs != null)
                    hash = hash * 59 + this.TradeClosedIDs.GetHashCode();
                if (this.CancellingTransactionID != null)
                    hash = hash * 59 + this.CancellingTransactionID.GetHashCode();
                if (this.CancelledTime != null)
                    hash = hash * 59 + this.CancelledTime.GetHashCode();
                if (this.ReplacesOrderID != null)
                    hash = hash * 59 + this.ReplacesOrderID.GetHashCode();
                if (this.ReplacedByOrderID != null)
                    hash = hash * 59 + this.ReplacedByOrderID.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        { 
            yield break;
        }
    }

}