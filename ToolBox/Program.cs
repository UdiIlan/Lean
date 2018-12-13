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
using System.Globalization;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.ToolBox.AlgoSeekFuturesConverter;
using QuantConnect.ToolBox.AlgoSeekOptionsConverter;
using QuantConnect.ToolBox.BitfinexDownloader;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.ToolBox.CryptoiqDownloader;
using QuantConnect.ToolBox.DukascopyDownloader;
using QuantConnect.ToolBox.FxcmDownloader;
using QuantConnect.ToolBox.FxcmVolumeDownload;
using QuantConnect.ToolBox.GDAXDownloader;
using QuantConnect.ToolBox.GoogleDownloader;
using QuantConnect.ToolBox.IBDownloader;
using QuantConnect.ToolBox.IEX;
using QuantConnect.ToolBox.IVolatilityEquityConverter;
using QuantConnect.ToolBox.KaikoDataConverter;
using QuantConnect.ToolBox.KrakenDownloader;
using QuantConnect.ToolBox.NseMarketDataConverter;
using QuantConnect.ToolBox.OandaDownloader;
using QuantConnect.ToolBox.QuandlBitfinexDownloader;
using QuantConnect.ToolBox.QuantQuoteConverter;
using QuantConnect.ToolBox.YahooDownloader;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var optionsObject = ToolboxArgumentParser.ParseArguments(args);
            if (optionsObject.Count == 0)
            {
                PrintMessageAndExit();
            }

            var targetApp = GetParameterOrExit(optionsObject, "app").ToLower();
            if (targetApp.Contains("download") || targetApp.EndsWith("dl"))
            {
                var fromDate = DateTime.ParseExact(GetParameterOrExit(optionsObject, "from-date"), "yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
                var resolution = optionsObject.ContainsKey("resolution") ? optionsObject["resolution"].ToString() : "";
                var tickers = optionsObject.ContainsKey("tickers")
                    ? (optionsObject["tickers"] as Dictionary<string, object>)?.Keys.ToList()
                    : new List<string>();
                var toDate = optionsObject.ContainsKey("to-date")
                    ? DateTime.ParseExact(optionsObject["to-date"].ToString(), "yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture)
                    : DateTime.UtcNow;
                switch (targetApp)
                {
                    case "gdaxdl":
                    case "gdaxdownloader":
                        GDAXDownloaderProgram.GDAXDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "cdl":
                    case "cryptoiqdownloader":
                        CryptoiqDownloaderProgram.CryptoiqDownloader(tickers, GetParameterOrExit(optionsObject, "exchange"), fromDate, toDate);
                        break;
                    case "ddl":
                    case "dukascopydownloader":
                        DukascopyDownloaderProgram.DukascopyDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "fdl":
                    case "fxcmdownloader":
                        FxcmDownloaderProgram.FxcmDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "fvdl":
                    case "fxcmvolumedownload":
                        FxcmVolumeDownloadProgram.FxcmVolumeDownload(tickers, resolution, fromDate, toDate);
                        break;
                    case "gdl":
                    case "googledownloader":
                        GoogleDownloaderProgram.GoogleDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "ibdl":
                    case "ibdownloader":
                        IBDownloaderProgram.IBDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "iexdl":
                    case "iexdownloader":
                        IEXDownloaderProgram.IEXDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "kdl":
                    case "krakendownloader":
                        KrakenDownloaderProgram.KrakenDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "odl":
                    case "oandadownloader":
                        OandaDownloaderProgram.OandaDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "qbdl":
                    case "quandlbitfinexdownloader":
                        QuandlBitfinexDownloaderProgram.QuandlBitfinexDownloader(fromDate, GetParameterOrExit(optionsObject, "api-key"));
                        break;
                    case "ydl":
                    case "yahoodownloader":
                        YahooDownloaderProgram.YahooDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "bfxdl":
                    case "bitfinexdownloader":
                        BitfinexDownloaderProgram.BitfinexDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    default:
                        PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
                        break;
                }
            }
            else
            {
                switch (targetApp)
                {
                    case "asfc":
                    case "algoseekfuturesconverter":
                        AlgoSeekFuturesProgram.AlgoSeekFuturesConverter(GetParameterOrExit(optionsObject, "date"));
                        break;
                    case "asoc":
                    case "algoseekoptionsconverter":
                        AlgoSeekOptionsConverterProgram.AlgoSeekOptionsConverter(GetParameterOrExit(optionsObject, "date"));
                        break;
                    case "ivec":
                    case "ivolatilityequityconverter":
                        IVolatilityEquityConverterProgram.IVolatilityEquityConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                                     GetParameterOrExit(optionsObject, "source-meta-dir"),
                                                                                     GetParameterOrExit(optionsObject, "destination-dir"),
                                                                                     GetParameterOrExit(optionsObject, "resolution"));
                        break;
                    case "kdc":
                    case "kaikodataconverter":
                        KaikoDataConverterProgram.KaikoDataConverter(GetParameterOrExit(optionsObject, "market"),
                                                                     GetParameterOrExit(optionsObject, "tick-type"),
                                                                     GetParameterOrExit(optionsObject, "source-dir"));
                        break;
                    case "nmdc":
                    case "nsemarketdataconverter":
                        NseMarketDataConverterProgram.NseMarketDataConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                             GetParameterOrExit(optionsObject, "destination-dir"));
                        break;
                    case "qqc":
                    case "quantquoteconverter":
                        QuantQuoteConverterProgram.QuantQuoteConverter(GetParameterOrExit(optionsObject, "destination-dir"),
                                                                       GetParameterOrExit(optionsObject, "source-dir"),
                                                                       GetParameterOrExit(optionsObject, "resolution"));
                        break;
                    case "cug":
                    case "coarseuniversegenerator":
                        CoarseUniverseGeneratorProgram.CoarseUniverseGenerator();
                        break;
                    default:
                        PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
                        break;
                }
            }
        }

        private static void PrintMessageAndExit(int exitCode = 0, string message = "")
        {
            if (!message.IsNullOrEmpty())
            {
                Console.WriteLine("\n" + message);
            }
            Console.WriteLine("\nUse the '--help' parameter for more information");
            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
            Environment.Exit(exitCode);
        }

        private static string GetParameterOrExit(IReadOnlyDictionary<string, object> optionsObject, string parameter)
        {
            if (!optionsObject.ContainsKey(parameter))
            {
                PrintMessageAndExit(1, "ERROR: REQUIRED parameter --" + parameter + "= is missing");
            }
            return optionsObject[parameter].ToString();
        }
    }
}
