using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    class HighVolatilityOptionsTradeAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {

        private bool registerForEOD = false;

        #region constants
        private static readonly string[] S_AND_P_500_SYMOLS = { "GOOG" }; // { "FB", "JPM", "XOM", "GOOG", "GOOGL", "PFE", "UNH", "VZ", "V", "PG", "BAC", "CVX", "INTC", "T", "CSCO", "WFC", "MRK", "HD", "KO", "MA", "BA", "CMCSA", "DIS", "PEP", "MCD", "C", "WMT", "ABBV", "ORCL", "PM", "MDT", "AMGN", "ABT", "DWDP", "ADBE", "MMM", "NFLX", "IBM", "LLY", "UNP", "AVGO", "CRM", "HON", "MO", "ACN", "PYPL", "COST", "UTX", "TMO", "CVS", "NKE", "TXN", "NVDA", "BKNG", "GILD", "LIN", "NEE", "BMY", "SBUX", "USB", "COP", "AXP", "AMT", "CAT", "LOW", "LMT", "UPS", "ANTM", "QCOM", "WBA", "CME", "MDLZ", "DUK", "BIIB", "BDX", "DHR", "GS", "ADP", "CB", "EOG", "GE", "SPG", "ISRG", "TJX", "SLB", "PNC", "CL", "CHTR", "CSX", "ESRX", "MS", "INTU", "SYK", "FOXA", "BSX", "CI", "D", "SCHW", "OXY", "CELG", "RTN", "ILMN", "SO", "CCI", "GD", "AGN", "BLK", "DE", "NOC", "FDX", "GM", "EXC", "ICE", "VRTX", "BK", "NSC", "ZTS", "HUM", "MMC", "SPGI", "PLD", "MPC", "MU", "ITW", "KMB", "ECL", "AEP", "EMR", "MET", "CTSH", "COF", "PSX", "AON", "PGR", "ATVI", "HCA", "WM", "BBT", "HPQ", "DAL", "TGT", "FIS", "PRU", "EW", "APD", "AMAT", "F", "ADI", "BAX", "AFL", "AIG", "TRV", "SRE", "SHW", "MAR", "PSA", "STZ", "VLO", "RHT", "SYY", "EL", "FISV", "EQIX", "ETN", "KMI", "ROST", "KHC", "JCI", "ADSK", "REGN", "ROP", "WMB", "ALL", "YUM", "DG", "PEG", "ORLY", "CNC", "XEL", "WELL", "LYB", "EBAY", "APC", "LUV", "EQR", "AVB", "ED", "TEL", "GLW", "ALXN", "EA", "HAL", "APH", "STI", "ADM", "PPG", "TWTR", "VFC", "CXO", "MCK", "MCO", "OKE", "DLR", "STT", "FOX", "IR", "PXD", "WEC", "AZO", "KR", "GIS", "VTR", "MNST", "ZBH", "CCL", "A", "TROW", "XLNX", "ES", "MTB", "DFS", "PAYX", "LRCX", "DTE", "HPE", "HLT", "FTV", "CLX", "PPL", "MSI", "PH", "PCAR", "WLTW", "CMI", "UAL", "DLTR", "SBAC", "IQV", "BXP", "NTRS", "ROK", "FE", "O", "EIX", "VRSK", "WY", "MKC", "SWK", "INFO", "CERN", "IP", "NUE", "RCL", "APTV", "NEM", "AWK", "ESS", "OMC", "AMD", "HRS", "AEE", "IDXX", "MCHP", "KEY", "TDG", "VRSN", "CHD", "NTAP", "SYF", "CAH", "TSN", "BLL", "FITB", "DXC", "EVRG", "GPN", "CBS", "AME", "RSG", "RMD", "CTL", "FLT", "ETR", "ALGN", "AMP", "FAST", "FCX", "RF", "K", "FANG", "MYL", "HSY", "CFG", "CMS", "MTD", "MXIM", "HIG", "ABMD", "CTAS", "LLL", "WAT", "KLAC", "GPC", "CAG", "LH", "TSS", "BBY", "CNP", "HBAN", "ULTA", "EXPE", "CTXS", "AAL", "IFF", "HCP", "SYMC", "DVN", "AJG", "MSCI", "PCG", "HST", "ARE", "ABC", "SNPS", "VMC", "MGM", "HES", "GWW", "MRO", "IT", "CBRE", "HSIC", "ANSS", "NRG", "DRI", "TXT", "L", "EXR", "EXPD", "COO", "CDNS", "SWKS", "CHRW", "CMA", "DHI", "AAP", "HRL", "WCG", "WDC", "VNO", "TTWO", "CINF", "DGX", "ANET", "LEN", "LNC", "XYL", "TAP", "MOS", "CMG", "ETFC", "APA", "EFX", "DOV", "MAA", "HOLX", "CBOE", "BR", "AKAM", "INCY", "SJM", "UDR", "WRK", "KMX", "LW", "PFG", "KEYS", "COG", "UHS", "MLM", "VAR", "TSCO", "REG", "BHGE", "LNT", "NBL", "FTNT", "NOV", "SIVB", "KSS", "FMC", "JKHY", "NCLH", "VIAB", "AES", "WYNN", "STX", "EMN", "PNW", "FFIV", "NWL", "DRE", "NDAQ", "TPR", "KSU", "NI", "IRM", "RJF", "CPRT", "HAS", "FRT", "M", "JNPR", "ALB", "BEN", "CF", "PKI", "DISCK", "NLSN", "TIF", "RE", "MAS", "HFC", "FTI", "IPG", "PKG", "HII", "ARNC", "JBHT", "ADS", "ZION", "URI", "SNA", "ALLE", "XRAY", "WU", "TMK", "LKQ", "SLG", "GRMN", "AVY", "BF.B", "ALK", "PVH", "MHK", "QRVO", "AIV", "CPB", "WHR", "PRGO", "DVA", "BWA", "RHI", "LB", "DISH", "JEC", "IVZ", "KIM", "XEC", "PHM", "SCG", "UNM", "PNR", "HP", "TRIP", "NKTR", "AOS", "FL", "FLIR", "HOG", "GPS", "FBHS", "RL", "PBCT", "FLS", "KORS", "HRB", "ROL", "JWN", "XRX", "HBI", "SEE", "JEF", "AMG", "MAC", "GT", "LEG", "NWSA", "AIZ", "FLR", "PWR", "IPGP", "DISCA", "MAT", "UAA", "UA", "BHF", "COTY", "NFX", "NWS" };
        private static readonly decimal M = 1.1M;
        private static readonly decimal P = 1.00M;
        private static readonly int D = 20;
        private static readonly int TRADE_PER_SYMBOL = 10;
        #endregion

        #region IRegressionAlgorithmDefinition 

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "8220"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "13.500%"},
            {"Expectancy", "-0.818"},
            {"Net Profit", "-13.517%"},
            {"Sharpe Ratio", "-29.354"},
            {"Loss Rate", "89%"},
            {"Win Rate", "11%"},
            {"Profit-Loss Ratio", "0.69"},
            {"Alpha", "-7.746"},
            {"Beta", "-0.859"},
            {"Annual Standard Deviation", "0.305"},
            {"Annual Variance", "0.093"},
            {"Information Ratio", "-24.985"},
            {"Tracking Error", "0.414"},
            {"Treynor Ratio", "10.413"},
            {"Total Fees", "$15207.00"}
        };

        #endregion

        #region QCAlgorithm

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            //SetStartDate(2017, 1, 1);
            //SetEndDate(2017, 12, 30);

            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 25);
            SetCash(1000000);

            foreach (string symbol in S_AND_P_500_SYMOLS)
            {
                var option = AddOption(symbol);//, Resolution.Daily);
                option.PriceModel = OptionPriceModels.BlackScholes();

                // set our strike/expiry filter for this option chain
                option.SetFilter((universe) => universe.WeeklysOnly().Expiration(TimeSpan.FromDays(0), TimeSpan.FromDays(7)));

                if (!this.registerForEOD)
                {

                    this.registerForEOD = true;

                    // Schedule an event to fire every trading day for a security
                    // The time rule here tells it to fire 10 minutes before market close
                    Schedule.On(DateRules.EveryDay(option.Symbol), TimeRules.BeforeMarketClose(option.Symbol, 15), () =>
                    {
                        Log("EveryDay 15 min before markets close: Fired at: " + Time);
                        this.liquidateExpiredOptions();
                    });
                }
            }

            // set the warm-up period for the pricing model
            SetWarmup(TimeSpan.FromDays(15));

            // set the benchmark to be the initial cash
            SetBenchmark((d) => 1000000);

        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            this.operate(slice);
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }

        #endregion

        #region class methods

        private void operate(Slice slice)
        {
            var options = slice.OptionChains;

            // sort option according to their impllied volatility
            var sortedOptions = options.OrderBy((x) => this.findVolatility(x.Value)).Select((x) => x.Value).ToArray();

            int size = sortedOptions.Length;
            Debug($"sortedOptions len: {size}");

            if (size > 0)
            {
                Debug($"best option is: {sortedOptions.First()}");
            }

            // trade the 5 option with the highest impllied volatility
            for (int i = 0; i < D && i < sortedOptions.Length; i++)
            {
                this.tradeOption(sortedOptions[i]);
            }
        }


        private decimal findVolatility(OptionChain chain)
        {

            decimal maxVolatility = chain.Max((x) => x.ImpliedVolatility);
            return maxVolatility;
        }

        private void tradeOption(OptionChain chain)
        {

#if DEBUG
            Debug("All contarcets in {chain.Underlying.Symbol} chain:");
            this.printChain(chain);
#endif

            var contracts = chain.Where((x) => (x.BidPrice + x.Strike) > chain.Underlying.Price * P).OrderByDescending((x) => x.ImpliedVolatility);
            if (!contracts.Any())
            {
                Debug($"Non OOM contacts found for: {chain.Underlying.Symbol}");
                return;
            }

#if DEBUG
            Debug($"Date: {Time} - trading for: {chain.Underlying.Symbol}");
#endif
            var calls = contracts.Where((x) => x.Right == OptionRight.Call && x.Expiry > Time.AddDays(1)).OrderByDescending((x) => x.Expiry);
            var puts = contracts.Where((x) => x.Right == OptionRight.Put && x.Expiry > Time.AddDays(1)).OrderByDescending((x) => x.Expiry);

            int totalPrice = TRADE_PER_SYMBOL / 2;

            // buy calls
            var bestCall = calls.First();
            int quantity = (int)Math.Floor(totalPrice / bestCall.BidPrice);
            LimitOrder(bestCall.Symbol, 1, bestCall.BidPrice);

            // buy puts
            var bestPut = puts.First();
            quantity = (int)Math.Floor(totalPrice / bestPut.BidPrice);
            LimitOrder(bestPut.Symbol, quantity, bestCall.BidPrice);
        }

        private void liquidateExpiredOptions()
        {
            foreach (var option in Portfolio.Values)
            {
                if (!option.Invested) continue;

                if (option.Type != SecurityType.Option)
                {
                    Liquidate(option.Symbol);
                    continue;
                }

                var contract = (Option)Portfolio.Securities[option.Symbol];
                if (contract.Expiry < Time.AddDays(1))
                {
                    Debug($"Date: {Time} - Liquidating {option.Symbol}");
                    Liquidate(contract.Symbol);
                }

            }
        }

        private void printSlice(Slice slice)
        {
            var options = slice.OptionChains;
            foreach (var optionChain in options)
            {
                printChain(optionChain.Value);
            }
        }

        private void printChain(OptionChain chain)
        {
            foreach (var contract in chain)
            {
                Debug($"Time: {contract.Time}, symbol: {chain.Underlying.Symbol} contract expried: {contract.Expiry}, strike: {contract.Strike}, stock price: {chain.Underlying.Price}, bid: {contract.BidPrice}, ask: {contract.AskPrice}, call/put: {contract.Right}, IV: {contract.ImpliedVolatility}");
            }        
        }

#endregion
    }

}
