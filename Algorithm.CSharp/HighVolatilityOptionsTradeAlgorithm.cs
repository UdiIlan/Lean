﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;
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
        // { "FB", "JPM", "XOM", "GOOG", "GOOGL", "PFE", "UNH", "VZ", "V", "PG", "BAC" }; 
        //private static readonly string[] S_AND_P_500_SYMOLS = { "MMM","ABT","ABBV","ABMD","ACN","ATVI","ADBE","AMD","AAP","AES","AMG","AFL","A","APD","AKAM","ALK","ALB","ARE","ALXN","ALGN","ALLE","AGN","ADS","LNT","ALL","GOOGL","GOOG","MO","AMZN","AEE","AAL","AEP","AXP","AIG","AMT","AWK","AMP","ABC","AME","AMGN","APH","APC","ADI","ANSS","ANTM","AON","AOS","APA","AIV","AAPL","AMAT","APTV","ADM","ARNC","ANET","AJG","AIZ","T","ADSK","ADP","AZO","AVB","AVY","BHGE","BLL","BAC","BK","BAX","BBT","BDX","BRK.B","BBY","BIIB","BLK","HRB","BA","BKNG","BWA","BXP","BSX","BHF","BMY","AVGO","BR","BF.B","CHRW","COG","CDNS","CPB","COF","CAH","KMX","CCL","CAT","CBOE","CBRE","CBS","CELG","CNC","CNP","CTL","CERN","CF","SCHW","CHTR","CVX","CMG","CB","CHD","CI","XEC","CINF","CTAS","CSCO","C","CFG","CTXS","CLX","CME","CMS","KO","CTSH","CL","CMCSA","CMA","CAG","CXO","COP","ED","STZ","COO","CPRT","GLW","COST","COTY","CCI","CSX","CMI","CVS","DHI","DHR","DRI","DVA","DE","DAL","XRAY","DVN","FANG","DLR","DFS","DISCA","DISCK","DISH","DG","DLTR","D","DOV","DWDP","DTE","DRE","DUK","DXC","ETFC","EMN","ETN","EBAY","ECL","EIX","EW","EA","EMR","ETR","EOG","EFX","EQIX","EQR","ESS","EL","EVRG","ES","RE","EXC","EXPE","EXPD","ESRX","EXR","XOM","FFIV","FB","FAST","FRT","FDX","FIS","FITB","FE","FISV","FLT","FLIR","FLS","FLR","FMC","FL","F","FTNT","FTV","FBHS","BEN","FCX","GPS","GRMN","IT","GD","GE","GIS","GM","GPC","GILD","GPN","GS","GT","GWW","HAL","HBI","HOG","HRS","HIG","HAS","HCA","HCP","HP","HSIC","HSY","HES","HPE","HLT","HFC","HOLX","HD","HON","HRL","HST","HPQ","HUM","HBAN","HII","IDXX","INFO","ITW","ILMN","IR","INTC","ICE","IBM","INCY","IP","IPG","IFF","INTU","ISRG","IVZ","IPGP","IQV","IRM","JKHY","JEC","JBHT","JEF","SJM","JNJ","JCI","JPM","JNPR","KSU","K","KEY","KEYS","KMB","KIM","KMI","KLAC","KSS","KHC","KR","LB","LLL","LH","LRCX","LW","LEG","LEN","LLY","LNC","LIN","LKQ","LMT","L","LOW","LYB","MTB","MAC","M","MRO","MPC","MAR","MMC","MLM","MAS","MA","MAT","MKC","MXIM","MCD","MCK","MDT","MRK","MET","MTD","MGM","KORS","MCHP","MU","MSFT","MAA","MHK","TAP","MDLZ","MNST","MCO","MS","MOS","MSI","MSCI","MYL","NDAQ","NOV","NKTR","NTAP","NFLX","NWL","NFX","NEM","NWSA","NWS","NEE","NLSN","NKE","NI","NBL","JWN","NSC","NTRS","NOC","NCLH","NRG","NUE","NVDA","ORLY","OXY","OMC","OKE","ORCL","PCAR","PKG","PH","PAYX","PYPL","PNR","PBCT","PEP","PKI","PRGO","PFE","PCG","PM","PSX","PNW","PXD","PNC","RL","PPG","PPL","PFG","PG","PGR","PLD","PRU","PEG","PSA","PHM","PVH","QRVO","PWR","QCOM","DGX","RJF","RTN","O","RHT","REG","REGN","RF","RSG","RMD","RHI","ROK","ROL","ROP","ROST","RCL","CRM","SBAC","SCG","SLB","STX","SEE","SRE","SHW","SPG","SWKS","SLG","SNA","SO","LUV","SPGI","SWK","SBUX","STT","SYK","STI","SIVB","SYMC","SYF","SNPS","SYY","TROW","TTWO","TPR","TGT","TEL","FTI","TXN","TXT","TMO","TIF","TWTR","TJX","TMK","TSS","TSCO","TDG","TRV","TRIP","FOXA","FOX","TSN","UDR","ULTA","USB","UAA","UA","UNP","UAL","UNH","UPS","URI","UTX","UHS","UNM","VFC","VLO","VAR","VTR","VRSN","VRSK","VZ","VRTX","VIAB","V","VNO","VMC","WMT","WBA","DIS","WM","WAT","WEC","WCG","WFC","WELL","WDC","WU","WRK","WY","WHR","WMB","WLTW","WYNN","XEL","XRX","XLNX","XYL","YUM","ZBH","ZION","ZTS","LW","MXIM","FANG","JKHY","KEYS","FTNT","ROL","WCG","ANET","CPRT","FLT","BR","HFC","TWTR","EVRG","ABMD","MSCI","TTWO","SIVB","NKTR","IPGP","HII","NCLH","CDNS","SBAC","Q","BHF","DRE","AOS","PKG","RMD","MGM","HLT","ALGN","ANSS","RE","INFO","IT","DXC","AMD","RJF","ARE","SNPS","DISH","REG","CBOE","INCY","IDXX","MAA","EVHC","COTY","COO","CHTR","MTD","FTV","LNT","ALB","FBHS","TDG","AJG","LKQ","DLR","ALK","AYI","GPN","ULTA","FL","HOLX","CNC","UDR","AWK","CXO","CFG","FRT","EXR","WLTW","CHD","CSRA","ILMN","SYF","HPE","VRSK","CMCSK","FOX","NWS","UAL","ATVI","SIG","PYPL","AAP","KHC","CPGX","JBHT","BXLT","QRVO","O","AAL","EQIX","SLG","HBI","HSIC","SWKS","HCA","ENDP","RCL","LVLT","URI","UHS","MNK","DISCK","MLM","AMG","XEC","AVGO","UA","NAVI","GOOGL","ESS","GMCR","TSCO","ADS","MHK","FB","GGP","ALLE","KORS","RIG","VRTX","AME","DAL","NLSN","FOXA","ZTS","GM","KSU","MAC","REGN","PVH","ABBV","DLPH","GRMN","DG","PETM","KRFT","ADT","PNR","LYB","ESV","STX","MNST","LRCX","ALXN","KMI","PSX","FOSL","CCI","WPX","TRIP","BWA","PRGO","DLTR","GAS","CBE","COL","AET","SRCL","EQT","CA","EVHC","ANDV","XL","GGP","DPS","TWX","RRC","AYI","MON","NAVI","WYN","CSRA","SIG","PDCO","CHK","SNI","BCR","LVLT","SPLS","DD","WFM","AN","RIG","BBBY","MUR","MNK","RAI","YHOO","TDC","R","MJN","TGNA","DNB","SWN","URBN","FTR","FSLR","HAR","LLTC","ENDP","PBI","SE","STJ","OI","LM","DO","HOT","EMC","JCI","CPGX","GAS","TE","CVC","BXLT","CCE","ARG","TWC","SNDK","ADT","GME","THC","CAM","POM","ESV","GMCR","CNX","PCL","PCP","BRCM","CB","FOSL","ALTR","CSC","SIAL","GNW","HCBK","JOY","HSP","PLL","DTV","NE","FDO","KRFT","ATI","TEG","QEP","LO","WIN","AGN","DNR","NBR","AVP","CFN","PETM","SWY","COV","BMS","JBL","BTU","GHC","RDC","X","FRX","IGT","LSI","BEAM","SLM","CLF","WPX","LIFE","ANF","JDSU","TER","MOLX","JCP","NYX","DELL","AMD","SAI","BMC","S","APOL","FHN","HNZ","DF","CVH","PCS","BIG","FII","TIE","RRD","CBE","SUN","ANR","LXK","DV","SHLD","GR","PGN","SLE","NVLS","MMI","EP","SVU","MHS","CEG","CPWR","TLAB","AKS","MWW","WFR","GAS","JNS","ITT","CEPH","NSM","MI","RSH","MEE","NOVL","GENZ","Q","MFE","AYE","KG","EK","ODP","NYT","SII","MIL","STR","XTO","BJS","RX","SGP","CBE","ACAS","JNY","ABK","TRB","DJ","AV","KSE","TSG","SBL","GLK","QTRN","BS","OI","GRA","CCK" };
        private static readonly string[] S_AND_P_500_SYMOLS = { "GOOG", "GOOGL" }; // { "FB", "JPM", "XOM", "GOOG", "GOOGL", "PFE", "UNH", "VZ", "V", "PG", "BAC", "CVX", "INTC", "T", "CSCO", "WFC", "MRK", "HD", "KO", "MA", "BA", "CMCSA", "DIS", "PEP", "MCD", "C", "WMT", "ABBV", "ORCL", "PM", "MDT", "AMGN", "ABT", "DWDP", "ADBE", "MMM", "NFLX", "IBM", "LLY", "UNP", "AVGO", "CRM", "HON", "MO", "ACN", "PYPL", "COST", "UTX", "TMO", "CVS", "NKE", "TXN", "NVDA", "BKNG", "GILD", "NEE", "BMY", "SBUX", "USB", "COP", "AXP", "AMT", "CAT", "LOW", "LMT", "UPS", "ANTM", "QCOM", "WBA", "CME", "MDLZ", "DUK", "BIIB", "BDX", "DHR", "GS", "ADP", "CB", "EOG", "GE", "SPG", "ISRG", "TJX", "SLB", "PNC", "CL", "CHTR", "CSX", "ESRX", "MS", "INTU", "SYK", "FOXA", "BSX", "CI", "D", "SCHW", "OXY", "CELG", "RTN", "ILMN", "SO", "CCI", "GD", "AGN", "BLK", "DE", "NOC", "FDX", "GM", "EXC", "ICE", "VRTX", "BK", "NSC", "ZTS", "HUM", "MMC", "SPGI", "PLD", "MPC", "MU", "ITW", "KMB", "ECL", "AEP", "EMR", "MET", "CTSH", "COF", "PSX", "AON", "PGR", "ATVI", "HCA", "WM", "BBT", "HPQ", "DAL", "TGT", "FIS", "PRU", "EW", "APD", "AMAT", "F", "ADI", "BAX", "AFL", "AIG", "TRV", "SRE", "SHW", "MAR", "PSA", "STZ", "VLO", "RHT", "SYY", "EL", "FISV", "EQIX", "ETN", "KMI", "ROST", "KHC", "JCI", "ADSK", "REGN", "ROP", "WMB", "ALL", "YUM", "DG", "PEG", "ORLY", "CNC", "XEL", "WELL", "LYB", "EBAY", "APC", "LUV", "EQR", "AVB", "ED", "TEL", "GLW", "ALXN", "EA", "HAL", "APH", "STI", "ADM", "PPG", "TWTR", "VFC", "CXO", "MCK", "MCO", "OKE", "DLR", "STT", "FOX", "IR", "PXD", "WEC", "AZO", "KR", "GIS", "VTR", "MNST", "ZBH", "CCL", "A", "TROW", "XLNX", "ES", "MTB", "DFS", "PAYX", "LRCX", "DTE", "HPE", "HLT", "FTV", "CLX", "PPL", "MSI", "PH", "PCAR", "WLTW", "CMI", "UAL", "DLTR", "SBAC", "IQV", "BXP", "NTRS", "ROK", "FE", "O", "EIX", "VRSK", "WY", "MKC", "SWK", "INFO", "CERN", "IP", "NUE", "RCL", "APTV", "NEM", "AWK", "ESS", "OMC", "AMD", "HRS", "AEE", "IDXX", "MCHP", "KEY", "TDG", "VRSN", "CHD", "NTAP", "SYF", "CAH", "TSN", "BLL", "FITB", "DXC", "EVRG", "GPN", "CBS", "AME", "RSG", "RMD", "CTL", "FLT", "ETR", "ALGN", "AMP", "FAST", "FCX", "RF", "K", "FANG", "MYL", "HSY", "CFG", "CMS", "MTD", "MXIM", "HIG", "ABMD", "CTAS", "LLL", "WAT", "KLAC", "GPC", "CAG", "LH", "TSS", "BBY", "CNP", "HBAN", "ULTA", "EXPE", "CTXS", "AAL", "IFF", "HCP", "SYMC", "DVN", "AJG", "MSCI", "PCG", "HST", "ARE", "ABC", "SNPS", "VMC", "MGM", "HES", "GWW", "MRO", "IT", "CBRE", "HSIC", "ANSS", "NRG", "DRI", "TXT", "L", "EXR", "EXPD", "COO", "CDNS", "SWKS", "CHRW", "CMA", "DHI", "AAP", "HRL", "WCG", "WDC", "VNO", "TTWO", "CINF", "DGX", "ANET", "LEN", "LNC", "XYL", "TAP", "MOS", "CMG", "ETFC", "APA", "EFX", "DOV", "MAA", "HOLX", "CBOE", "BR", "AKAM", "INCY", "SJM", "UDR", "WRK", "KMX", "LW", "PFG", "KEYS", "COG", "UHS", "MLM", "VAR", "TSCO", "REG", "BHGE", "LNT", "NBL", "FTNT", "NOV", "SIVB", "KSS", "FMC", "JKHY", "NCLH", "VIAB", "AES", "WYNN", "STX", "EMN", "PNW", "FFIV", "NWL", "DRE", "NDAQ", "TPR", "KSU", "NI", "IRM", "RJF", "CPRT", "HAS", "FRT", "M", "JNPR", "ALB", "BEN", "CF", "PKI", "DISCK", "NLSN", "TIF", "RE", "MAS", "HFC", "FTI", "IPG", "PKG", "HII", "ARNC", "JBHT", "ADS", "ZION", "URI", "SNA", "ALLE", "XRAY", "WU", "TMK", "LKQ", "SLG", "GRMN", "AVY", "BF.B", "ALK", "PVH", "MHK", "QRVO", "AIV", "CPB", "WHR", "PRGO", "DVA", "BWA", "RHI", "LB", "DISH", "JEC", "IVZ", "KIM", "XEC", "PHM", "SCG", "UNM", "PNR", "HP", "TRIP", "NKTR", "AOS", "FL", "FLIR", "HOG", "GPS", "FBHS", "RL", "PBCT", "FLS", "KORS", "HRB", "ROL", "JWN", "XRX", "HBI", "SEE", "JEF", "AMG", "MAC", "GT", "LEG", "NWSA", "AIZ", "FLR", "PWR", "IPGP", "DISCA", "MAT", "UAA", "UA", "BHF", "COTY", "NFX", "NWS" };
        private static readonly decimal M = 0.5M;
        private static readonly decimal P = 1.01M;
        private static readonly int D = 5;
        private static readonly int TRADE_PER_SYMBOL = 1000;
        //private static readonly int MAX_IV = 4;
        //private static readonly int MAX_EXPIRATION_DAYS = 8;
        //private static readonly int MIN_EXPIRATION_DAYS = 1;
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
            //SetStartDate(2013, 11, 1);
            //SetEndDate(2018, 9, 30);

            SetStartDate(2015, 5, 1);
            SetEndDate(2015, 7, 30);
            SetCash(1000000000);

            foreach (string symbol in S_AND_P_500_SYMOLS)
            {
                var option = AddOption(symbol, Resolution.Daily, null, false);
                option.PriceModel = OptionPriceModels.BlackScholes();

                // set our strike/expiry filter for this option chain
                //option.SetFilter((universe) => universe.WeeklysOnly().Expiration(TimeSpan.FromDays(0), TimeSpan.FromDays(7)));
                //option.SetFilter(TimeSpan.FromDays(0), TimeSpan.FromDays(7));

                if (!this.registerForEOD)
                {

                    this.registerForEOD = true;

                    // Schedule an event to fire every trading day for a security
                    // The time rule here tells it to fire 10 minutes before market close
                    Schedule.On(DateRules.EveryDay(option.Symbol), TimeRules.BeforeMarketClose(option.Symbol, 15), () =>
                    {
                        Log("EveryDay 15 min before markets close: Fired at: " + Time);
                        this.LiquidateExpiredOptions();
                    });
                }
            }

            // set the warm-up period for the pricing model
            SetWarmup(TimeSpan.FromDays(15));

            // set the benchmark to be the initial cash
            SetBenchmark((d) => 1000000000);

        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            Operate(slice);
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

        private void Operate(Slice slice)
        {
            var tradableOptions = GetTradableOptions(slice.OptionChains);

            if (!tradableOptions.Any()) return;

            // sort tradable option according to their impllied volatility
            var sortedOptions = tradableOptions.OrderBy((x) => x.IV, OrderByDirection.Descending);
            Debug($"best option is: {sortedOptions.FirstOrDefault()}");

            // trade the first D options with the highest impllied volatility
            for (int i = 0; i < D && i < sortedOptions.Count(); i++)
            {
                TradableOption option = sortedOptions.ElementAt(i);
                Debug($"High IV option is: {option.Symbol}, IV: {option.IV}");
                TradeOptionContract(option.Call);
                TradeOptionContract(option.Put);
            }
        }


        private void TradeOptionContract(OptionContract contract)
        {

            if (contract == null || contract.BidPrice == 0) return;

            int totalPrice = TRADE_PER_SYMBOL / 2;
            decimal price = (contract.BidPrice + contract.AskPrice) / 2;
            int quantity = (int)Math.Floor(totalPrice / price);
            if (quantity > 0 && price > 0) LimitOrder(contract.Symbol, quantity, price);
        }

        private List<TradableOption> GetTradableOptions(OptionChains chains)
        {
            List<TradableOption> res = new List<TradableOption>();

            chains.Values.ToList().ForEach((chain) =>
            {
                //var contracts = chain.Contracts.Values.Where((x) =>
                //{
                //    return
                //     x.BidPrice > M &
                //     x.BidPrice > 0 &
                //     x.ImpliedVolatility < MAX_IV &
                //     x.Expiry >= Time.AddDays(MIN_EXPIRATION_DAYS) &
                //     x.Expiry <= Time.AddDays(MAX_EXPIRATION_DAYS);
                //});

                var contracts = chain.Contracts.Values;

                var calls = contracts.Where((x) => x.Right == OptionRight.Call);
                var puts = contracts.Where((x) => x.Right == OptionRight.Put);

                // get symbol IV out of the closest put&call avarage IV.
                OptionContract closestCall = calls.MinBy((x) => Math.Abs(chain.Underlying.Price - x.Strike)).FirstOrDefault();
                OptionContract closestPut = puts.MinBy((x) => Math.Abs(chain.Underlying.Price - x.Strike)).FirstOrDefault();
                decimal IV = closestCall != null & closestPut != null ? (closestCall.ImpliedVolatility + closestPut.ImpliedVolatility) / 2 : 0;

                OptionContract bestCall = calls.Where((x) => (x.BidPrice + x.Strike) > chain.Underlying.Price * P && x.BidPrice > M).OrderByDescending((x) => x.Expiry).FirstOrDefault();
                OptionContract bestPut = puts.Where((x) => (x.Strike - x.BidPrice) < chain.Underlying.Price * (2 - P) && x.BidPrice > M).OrderByDescending((x) => x.Expiry).FirstOrDefault();

                res.Add(new TradableOption()
                {
                    Symbol = chain.Symbol,
                    Call = bestCall,
                    Put = bestPut,
                    IV = IV
                });

            });
            return res;
        }

        private void LiquidateExpiredOptions()
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

        private void PrintSlice(Slice slice)
        {
            var options = slice.OptionChains;
            foreach (var optionChain in options)
            {
                PrintChain(optionChain.Value);
            }
        }

        private void PrintChain(OptionChain chain)
        {
            foreach (var contract in chain)
            {
                Debug($"Time: {contract.Time}, symbol: {chain.Underlying.Symbol} contract expried: {contract.Expiry}, strike: {contract.Strike}, stock price: {chain.Underlying.Price}, bid: {contract.BidPrice}, ask: {contract.AskPrice}, call/put: {contract.Right}, IV: {contract.ImpliedVolatility}");
            }
        }

        #endregion
    }

    class TradableOption
    {

        public Symbol Symbol
        {
            get; set;
        }

        public decimal IV
        {
            get; set;
        }

        public OptionContract Put
        {
            get; set;
        }

        public OptionContract Call
        {
            get; set;
        }

    }

}
