"""
for s&p 500 weekly options on every day:
    close all positions from previous day in ask price
    sort by IV
    trade 5 options of stocks with the most volatile options:
        consider only in market options (according to bid + strike > price * P), sort by IV
        filter out options that expire today, sort by closest strike
        sell best call and best put in bid price
"""
import pandas as pd
import zipfile
import re
import os
import math
import time
import datetime
import json
import matplotlib.pylab as plt
import logging
from datetime import date
from utils import parse_date
import sys

global log

SOURCE_DIR = '.\\Source'
SNP_SYMBOLS_FILE_PATH = ".\\snp500.txt"
DAILY_TRADE_OPTIONS = 5
PRICE_FACTOR = 1
TRADE_PER_SYMBOL = 5000
DAYS_TO_PROCESS = 10000
MINIMUM_BID = 0.5
EXPECTED_STOCK_CHANGE_RATIO = [0, 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09, 0.1]
BID_RATIO = [1, 0.5]
MAX_TRADE_BATCH = 1
MAX_SYMBOLS_TO_CHECK = 300


def process_source_dir(source_dir, snp_symbols, is_compressed, results_dir, start_date, end_date):
    start_time = datetime.datetime.now()
    input_files = []
    total_profit = dict()

    # collect files form sorce folder by the given start_date/end_date
    if not is_compressed:
        input_files = get_csv_files_in_folder(source_dir, start_date, end_date)
    else:
        zip_files = get_zip_files_in_folder(source_dir, start_date, end_date)
        for curr_file in zip_files:
            file_path = os.path.join(source_dir, curr_file)
            files_in_curr_zip = get_files_from_zip_by_date(file_path)
            for curr_zipped_file in files_in_curr_zip:
                input_files.append({'zip': curr_file, 'data': files_in_curr_zip[curr_zipped_file]})

    day_index = 0
    open_positions = dict()
    daily_status = dict()

    # initialize all simulation parameters data structures
    for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
        open_positions[curr_stock_ratio] = dict()
        total_profit[curr_stock_ratio] = dict()
        for curr_bid_ratio in BID_RATIO:
            open_positions[curr_stock_ratio][curr_bid_ratio] = dict()
            total_profit[curr_stock_ratio][curr_bid_ratio] = dict()
            for batch_index in range(MAX_TRADE_BATCH):
                total_profit[curr_stock_ratio][curr_bid_ratio][batch_index] = 0
                open_positions[curr_stock_ratio][curr_bid_ratio][batch_index] = dict()

    prev_zip = ''
    zip_file_obj = None
    all_trades = {}

    # 
    for input_file in input_files:
        day_index += 1
        options_start = time.time()
        if day_index > DAYS_TO_PROCESS:
             break
        if not is_compressed:
            log.info(f'Processing {input_file}')
            _, year, month, day = parse_filename(input_file)
            options_data = pd.read_csv(os.path.join(source_dir, input_file))
        else:
            if input_file['zip'] != prev_zip:
                prev_zip = input_file['zip']
                zip_file_obj = zipfile.ZipFile(os.path.join(source_dir, prev_zip))
            date_info = input_file['data']
            options_file = date_info['options']
            options_data = pd.read_csv(zip_file_obj.open(options_file))
            day = date_info['day']
            month = date_info['month']
            year = date_info['year']
        (today_income, today_expenses, curr_trade) = process_options_file(options_data, year, month, day,
                                                                          snp_symbols, open_positions,
                                                                          EXPECTED_STOCK_CHANGE_RATIO, BID_RATIO,
                                                                          MAX_TRADE_BATCH)
        day_key = datetime.datetime(year=year, month=month, day=day)
        day_key_str = f'{day:02}/{month:02}/{year}'
        all_trades[f'{day:02}/{month:02}/{year}'] = curr_trade
        daily_status[day_key_str] = dict()
        for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
            daily_status[day_key_str][curr_stock_ratio] = dict()
            for curr_bid_ratio in BID_RATIO:
                for batch_index in range(MAX_TRADE_BATCH):
                    for expiration_date in curr_trade[curr_stock_ratio][curr_bid_ratio][batch_index]:
                        if expiration_date not in open_positions[curr_stock_ratio][curr_bid_ratio][batch_index]:
                            open_positions[curr_stock_ratio][curr_bid_ratio][batch_index][expiration_date] = \
                                curr_trade[curr_stock_ratio][curr_bid_ratio][batch_index][expiration_date].copy()
                        else:
                            open_positions[curr_stock_ratio][curr_bid_ratio][batch_index][expiration_date] += \
                                curr_trade[curr_stock_ratio][curr_bid_ratio][batch_index][expiration_date]
                    income = today_income[curr_stock_ratio][curr_bid_ratio][batch_index]
                    expenses = today_expenses[curr_stock_ratio][curr_bid_ratio][batch_index]
                    total_profit[curr_stock_ratio][curr_bid_ratio][batch_index] += \
                        (income - expenses)

                    # Going over all of the batches including all open positions that may have existed from before
                    daily_status[day_key_str][curr_stock_ratio][curr_bid_ratio] = dict()
                    for batch_index in range(MAX_TRADE_BATCH):
                        # Calculating again because maybe there are more batches because of the open positions
                        income = today_income[curr_stock_ratio][curr_bid_ratio][batch_index]
                        expenses = today_expenses[curr_stock_ratio][curr_bid_ratio][batch_index]
                        open_positions_cost = 0
                        if batch_index in open_positions[curr_stock_ratio][curr_bid_ratio]:
                            open_positions_cost = calc_positions_cost(
                                open_positions[curr_stock_ratio][curr_bid_ratio][batch_index])
                        profit = 0
                        if batch_index in total_profit[curr_stock_ratio][curr_bid_ratio]:
                            profit = total_profit[curr_stock_ratio][curr_bid_ratio][batch_index]
                        current_status = profit - open_positions_cost
                        log.info(f'{curr_stock_ratio},{curr_bid_ratio},{batch_index}: profit for {day}/{month}/{year}'
                              f' is {income - expenses}, '
                              f'total profit after {day_index} '
                              f'days is {current_status}')
                        daily_status[day_key_str][curr_stock_ratio][curr_bid_ratio][batch_index] = current_status
        log.info(f'Processing options for {day}/{month}/{year} took {time.time() - options_start} seconds')

    # Reduce remaining open positions
    for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
        for curr_bid_ratio in BID_RATIO:
            for batch_index in total_profit[curr_stock_ratio][curr_bid_ratio]:
                total_profit[curr_stock_ratio][curr_bid_ratio][batch_index] -= \
                    calc_positions_cost(open_positions[curr_stock_ratio][curr_bid_ratio][batch_index])
                log.info(f'{curr_stock_ratio},{curr_bid_ratio},{batch_index} Total profit: '
                      f'{total_profit[curr_stock_ratio][curr_bid_ratio][batch_index]}')


    log.info("Daily statuses %s", daily_status)
    plot_results(daily_status, EXPECTED_STOCK_CHANGE_RATIO, BID_RATIO, MAX_TRADE_BATCH, start_time, results_dir)
    all_trades_separate_dates = dict()
    for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
        all_trades_separate_dates[curr_stock_ratio] = dict()
        for curr_bid_ratio in BID_RATIO:
            all_trades_separate_dates[curr_stock_ratio][curr_bid_ratio] = dict()
    
    for curr_date in all_trades:
        for curr_stock_ratio in all_trades[curr_date]:
            for curr_bid_ratio in all_trades[curr_date][curr_stock_ratio]:
                all_trades_separate_dates[curr_stock_ratio][curr_bid_ratio][curr_date] = []
                for batch_index in all_trades[curr_date][curr_stock_ratio][curr_bid_ratio]:
                    if batch_index == 0:
                        for curr_trade_date in list(
                                all_trades[curr_date][curr_stock_ratio][curr_bid_ratio][batch_index].keys()):
                            new_key = f'{curr_trade_date.day:02}/{curr_trade_date.month:02}/{curr_trade_date.year}'
                            all_trades[curr_date][curr_stock_ratio][curr_bid_ratio][batch_index][new_key] = \
                                all_trades[curr_date][curr_stock_ratio][curr_bid_ratio][batch_index][curr_trade_date]
                            del all_trades[curr_date][curr_stock_ratio][curr_bid_ratio][batch_index][curr_trade_date]
                            for curr_option in all_trades[curr_date][curr_stock_ratio][curr_bid_ratio][batch_index][new_key]:
                                expiration = curr_option['expiration']
                                curr_option['expiration'] = f'{expiration.day:02}/{expiration.month:02}/{expiration.year}'
                                all_trades_separate_dates[curr_stock_ratio][
                                    curr_bid_ratio][curr_date].append(curr_option)

    with open(f'{os.path.join(results_dir, "DailyStatus.json")}', 'w') as outfile:
        json.dump(daily_status, outfile, default=myconverter)
    with open(f'{os.path.join(results_dir, "Trades.json")}', 'w') as outfile:
        json.dump(all_trades_separate_dates, outfile, default=myconverter)
    with open(f'{os.path.join(results_dir, "TotalProfit.json")}', 'w') as outfile:
        json.dump(total_profit, outfile, default=myconverter)


def myconverter(o):
    if isinstance(o, datetime.datetime):
        return o.__str__()


def calc_positions_cost(positions):
    total_cost = 0
    for curr_date in positions:
        for curr_position in positions[curr_date]:
            total_cost += curr_position['price'] * curr_position['size']

    return  total_cost


def get_snp_symbols(snp_500_filename):
    # snp_500_filename = "./snp500.txt"
    snp_set = set()
    with open(snp_500_filename) as snp_file:
        for symbol in snp_file:
            snp_set.add(symbol.strip())
    return snp_set


def get_zip_files(zip_path):
    result = []
    log.info(zip_path)
    with zipfile.ZipFile(zip_path, "r") as f:
        for name in f.namelist():
            result.append(name)
    return result


def get_zip_files_in_folder(folder_path, start_date, end_date):
    result = []
    for filename in os.listdir(folder_path):
        if '.zip' in filename:
            m = re.search('(.+)_(.+)\.zip', filename)
            year = int(m.group(1))
            month = time.strptime(m.group(2), '%B').tm_mon
            file_date = date(year, month, 1)
            if start_date is not None and end_date is not None:
                if file_date >= start_date and file_date <= end_date:
                    result.append(filename)
            else:
                result.append(filename)
    result = sorted(result, key=filename_to_sort_number)
    return result


def get_csv_files_in_folder(folder_path, start_date, end_date):
    result = []
    for filename in os.listdir(folder_path):
        if '.csv' in filename and 'option' in filename:
            m = re.search('(.+)_(.+)\.csv', filename)
            date_part = m.group(2)
            year = int(date_part[0:4])
            month = int(date_part[4:6])
            day = int(date_part[6:8])
            file_date = date(year, month, day)
            if file_date >= start_date and file_date <= end_date:
                result.append(filename)
    result = sorted(result)
    return result


def filename_to_sort_number(filename):
    m = re.search('(.+)_(.+)\.zip', filename)
    year = int(m.group(1))
    month = time.strptime(m.group(2), '%B').tm_mon
    return year * 100 + month


def get_files_from_zip_by_date(zip_path):
    files_in_date = {}
    csv_files_from_zip = get_zip_files(zip_path)
    for curr_csv in csv_files_from_zip:
        file_type, year, month, day = parse_filename(curr_csv)
        date_key = f'{year}_{month}_{day}'
        if file_type in ['stockquotes', 'options']:
            if date_key not in files_in_date:
                files_in_date[date_key] = {'year': year, 'month': month, 'day': day}
            files_in_date[date_key][file_type] = curr_csv
    return files_in_date


def parse_filename(filename):
    m = re.search('(.+)_(\d\d\d\d)(\d\d)(\d\d)\.+', filename)
    file_type = m.group(1)
    year = int(m.group(2))
    month = int(m.group(3))
    day = int(m.group(4))
    return file_type, year, month, day


def get_options_by_iv(options_data):
    options_data['OptionPriceDifference'] = options_data.apply(lambda row: abs(row['UnderlyingPrice'] -
                                                                               row['Strike']), axis=1)
    symbol_grouped_options = options_data.groupby('UnderlyingSymbol')

    # Filter options with strike closest to stock price
    min_option_difference_index = \
        symbol_grouped_options['OptionPriceDifference'].transform(min) >= options_data['OptionPriceDifference']
    closest_to_strike_options = options_data[min_option_difference_index].copy()

    # Calculate IV as average between call and put of closest to strike for each expiration
    average_iv_expiration_grouped_closest_to_strike_options = closest_to_strike_options.groupby(
        ['UnderlyingSymbol', 'Expiration'])['IV'].mean()
    average_iv_expiration_grouped_closest_to_strike_options.sort_values(inplace=True, ascending=False)
    return average_iv_expiration_grouped_closest_to_strike_options


def process_options_file(options_data, year, month, day, snp_symbols, current_options, ratio_params, bid_ratios,
                         trade_batches):
    log.info(f'Handling options for {day}/{month}/{year}')
    zip_date = datetime.datetime(year=year, month=month, day=day)

    snp_options = filter_snp_symbols(options_data, snp_symbols)  # options_data[options_data.UnderlyingSymbol.isin(snp_symbols)].copy()
    try:
        snp_options['Expiration'] = pd.to_datetime(snp_options['Expiration'], format='%m/%d/%Y')
    except Exception as e:
        snp_options['Expiration'] = pd.to_datetime(snp_options['Expiration'], format='%Y/%m/%d')

    # Only options that expire a week from today
    snp_options = filter_tradable_options(snp_options, zip_date, 1, 8, 4)
    average_iv_expiration_grouped_closest_to_strike_options = get_options_by_iv(snp_options)

    trade_index = 0
    all_today_trade = dict()
    today_income = dict()
    daily_trades_num = dict()
    trade_options_per_ratio = dict()
    ratio_trade_indexes = dict()
    for curr_stock_ratio in ratio_params:
        today_income[curr_stock_ratio] = dict()
        all_today_trade[curr_stock_ratio] = dict()
        daily_trades_num[curr_stock_ratio] = 0
        trade_options_per_ratio[curr_stock_ratio] = []
        ratio_trade_indexes[curr_stock_ratio] = 0
        for curr_bid_ratio in bid_ratios:
            today_income[curr_stock_ratio][curr_bid_ratio] = dict()
            all_today_trade[curr_stock_ratio][curr_bid_ratio] = dict()
            for batch_index in range(trade_batches):
                today_income[curr_stock_ratio][curr_bid_ratio][batch_index] = 0
                all_today_trade[curr_stock_ratio][curr_bid_ratio][batch_index] = dict()
    symbol_index = 0
    for (trade_group, curr_iv) in average_iv_expiration_grouped_closest_to_strike_options.iteritems():
        symbol_index += 1
        if symbol_index > MAX_SYMBOLS_TO_CHECK:
            break
        min_trade_in_ratio = min(daily_trades_num)
        min_batches = int(min_trade_in_ratio / DAILY_TRADE_OPTIONS)
        if min_batches >= MAX_TRADE_BATCH:
            break
        trade_symbol = trade_group[0]
        symbol_option_chain = snp_options[snp_options.UnderlyingSymbol == trade_symbol]

        # Filtering only out of market options that are actually traded and cost more then MINIMUN_BID
        oom_symbol_options = symbol_option_chain[symbol_option_chain.Volume > 0]
        oom_symbol_options = oom_symbol_options[oom_symbol_options.Bid > MINIMUM_BID]
        oom_symbol_options = oom_symbol_options[oom_symbol_options.Expiration > zip_date + datetime.timedelta(days=1)]
        oom_symbol_options = oom_symbol_options[oom_symbol_options.Expiration <= zip_date + datetime.timedelta(days=7)]
        calls = oom_symbol_options[oom_symbol_options.Type == 'call']
        puts = oom_symbol_options[oom_symbol_options.Type == 'put']
        calls = calls[calls.Strike + calls.Bid > calls.UnderlyingPrice]
        puts = puts[puts.Strike - puts.Bid < puts.UnderlyingPrice]
        if calls.shape[0] == 0 or puts.shape[0] == 0:
            #log.info(f'No tradable options for symbol {trade_symbol}')
            pass
        else:
            #       Start from strike. If stock price increases by K then the option breaks even.
            #       Example: stock = 100$, at minimal expiration, call_100=8$, call_105=4$, call_108=3$, call_110=1$
            #                At 10% increase call_100 loses 2$, call_105 loses 1$, call_108,110 earn 1$.
            #                Call 108 will be selected because it's the first that breaks even.
            #       For calls: first option (by strike) where Strike + OptionPrice > StockPrice * (1 + K)
            #       For Puts: first option (by strike) where Strike - OptionPrice < StockPrice * (1 - K)
            #       For pricing take bid
            max_trades_per_ratio = MAX_TRADE_BATCH * DAILY_TRADE_OPTIONS
            at_least_one_ratio_left_to_trade = False
            for curr_stock_ratio in ratio_params:
                if daily_trades_num[curr_stock_ratio] < max_trades_per_ratio:
                    at_least_one_ratio_left_to_trade = True
                    calls = calls[calls.Strike + 0.5 * calls.Bid + 0.5 * calls.Ask >
                                  calls.UnderlyingPrice * (1 + curr_stock_ratio)].copy()
                    puts = puts[puts.Strike - 0.5 * puts.Bid - 0.5 * puts.Ask <
                                puts.UnderlyingPrice * (1 - curr_stock_ratio)].copy()
                    calls.sort_values(by='Strike', inplace=True, ascending=True)
                    puts.sort_values(by='Strike', inplace=True, ascending=False)
                    if calls.shape[0] > 0 and puts.shape[0] > 0:
                        daily_trades_num[curr_stock_ratio] += 1
                        trade_options_per_ratio[curr_stock_ratio].append({'calls': calls, 'puts': puts})
            #if not at_least_one_ratio_left_to_trade:
            #    break

    for curr_stock_ratio in ratio_params:
        if daily_trades_num[curr_stock_ratio] == 0:
            log.info(f'{day}/{month}/{year} Not trading in ratio={curr_stock_ratio}')
        else:
            trade_index = -1
            batches_in_ratio = int(daily_trades_num[curr_stock_ratio] / DAILY_TRADE_OPTIONS)
            if daily_trades_num[curr_stock_ratio] % DAILY_TRADE_OPTIONS > 0:
                batches_in_ratio += 1
            log.info(f'Trading in {batches_in_ratio} batches for ratio {curr_stock_ratio}, total '
                  f'{daily_trades_num[curr_stock_ratio]} symbols {len(trade_options_per_ratio[curr_stock_ratio])}')
            for curr_trade in trade_options_per_ratio[curr_stock_ratio]:
                trade_index += 1
                trade_batch_index = int(trade_index / DAILY_TRADE_OPTIONS)
                if trade_batch_index + 1 < batches_in_ratio:
                    batch_symbols = DAILY_TRADE_OPTIONS
                else:
                    batch_symbols = daily_trades_num[curr_stock_ratio] % DAILY_TRADE_OPTIONS
                    if batch_symbols == 0:
                        batch_symbols = DAILY_TRADE_OPTIONS
                symbol_trade_in_ratio = TRADE_PER_SYMBOL / batch_symbols
                log.info(f'{day}/{month}/{year} {trade_index} trading in ratio={curr_stock_ratio} in '
                      f'{batch_symbols} symbols in batch {trade_batch_index + 1}, '
                      f'{symbol_trade_in_ratio} USD per symbol')
                if trade_batch_index > MAX_TRADE_BATCH:
                    break
                ratio_trade_indexes[curr_stock_ratio] += 1
                calls = curr_trade['calls']
                puts = curr_trade['puts']
                for curr_bid_ratio in bid_ratios:
                    option_trade_symbols = [{'symbol': calls['OptionSymbol'].iloc[0],
                                             'price': curr_bid_ratio * calls['Bid'].iloc[0] +
                                                      (1 - curr_bid_ratio) * calls['Ask'].iloc[0],
                                             'type': 'call',
                                             'expiration': calls['Expiration'].iloc[0],
                                             'underlying_symbol': calls['UnderlyingSymbol'].iloc[0],
                                             'strike': calls['Strike'].iloc[0]},
                                            {'symbol': puts['OptionSymbol'].iloc[0],
                                             'price': curr_bid_ratio * puts['Bid'].iloc[0] + (
                                                     1 - curr_bid_ratio) * puts['Ask'].iloc[0],
                                             'type': 'put',
                                             'expiration': puts['Expiration'].iloc[0],
                                             'underlying_symbol': puts['UnderlyingSymbol'].iloc[0],
                                             'strike': puts['Strike'].iloc[0]}]
                    for curr_option_trade_symbol in option_trade_symbols:
                        trade_size = int(math.floor((symbol_trade_in_ratio / len(option_trade_symbols)) /
                                                    curr_option_trade_symbol['price']))
                        curr_option_trade_symbol['size'] = trade_size
                        log.info(f'{curr_stock_ratio},{curr_bid_ratio},{trade_batch_index}: Writing '
                              f'{trade_size} * {curr_option_trade_symbol["symbol"]} for '
                              f'{curr_option_trade_symbol["price"]}')
                        today_income[curr_stock_ratio][curr_bid_ratio][trade_batch_index] += \
                            curr_option_trade_symbol['price'] * trade_size
                        if curr_option_trade_symbol['expiration'] not in \
                                all_today_trade[curr_stock_ratio][curr_bid_ratio][trade_batch_index]:
                            all_today_trade[curr_stock_ratio][curr_bid_ratio][trade_batch_index][
                                curr_option_trade_symbol['expiration']] = []
                        all_today_trade[curr_stock_ratio][curr_bid_ratio][trade_batch_index][
                            curr_option_trade_symbol['expiration']].append(
                            curr_option_trade_symbol)

    # When option expires pay the difference Strike and StockPrice
    today_expenses = dict()
    for curr_stock_ratio in ratio_params:
        today_expenses[curr_stock_ratio] = dict()
        for curr_bid_ratio in bid_ratios:
            today_expenses[curr_stock_ratio][curr_bid_ratio] = dict()
            for batch_index in current_options[curr_stock_ratio][curr_bid_ratio]:
                today_expenses[curr_stock_ratio][curr_bid_ratio][batch_index] = 0
                if zip_date in current_options[curr_stock_ratio][curr_bid_ratio][batch_index]:
                    missing_symbols = []
                    for curr_traded_symbol in current_options[curr_stock_ratio][curr_bid_ratio][batch_index][zip_date]:
                        symbol_data = options_data[options_data.UnderlyingSymbol ==
                                                   curr_traded_symbol['underlying_symbol']]
                        if symbol_data.shape[0] == 0:
                            log.info(f'{zip_date},{curr_stock_ratio}{curr_bid_ratio}'
                                  f' Missing symbol: {curr_traded_symbol["symbol"]}')
                            missing_symbols.append(curr_traded_symbol['symbol'])
                        else:
                            underlying_price = symbol_data['UnderlyingPrice'].iloc[0]
                            strike_price = curr_traded_symbol['strike']
                            pay_per_option = 0
                            if curr_traded_symbol['type'] == 'call' and underlying_price > strike_price:
                                pay_per_option = underlying_price - strike_price
                            elif curr_traded_symbol['type'] == 'put' and underlying_price < strike_price:
                                pay_per_option = strike_price - underlying_price
                            symbol_expenses = pay_per_option * curr_traded_symbol['size']
                            today_expenses[curr_stock_ratio][curr_bid_ratio][batch_index] += symbol_expenses
                            log.info(f'{curr_stock_ratio},{curr_bid_ratio},{batch_index}: For '
                                  f'{curr_traded_symbol["symbol"]} the underlying price is {underlying_price}, paying '
                                  f'{pay_per_option} for {curr_traded_symbol["size"]} options, total to pay is '
                                  f'{symbol_expenses}')
                    if len(missing_symbols) == 0:
                        del current_options[curr_stock_ratio][curr_bid_ratio][batch_index][zip_date]
                    else:
                        current_options[curr_stock_ratio][curr_bid_ratio][batch_index][zip_date][:] = \
                            [curr_traded_symbol for curr_traded_symbol in
                             current_options[curr_stock_ratio][curr_bid_ratio][batch_index][zip_date] if not
                             curr_traded_symbol['symbol'] in missing_symbols]
                        if len(current_options[curr_stock_ratio][curr_bid_ratio][batch_index][zip_date]) == 0:
                            del current_options[curr_stock_ratio][curr_bid_ratio][batch_index][zip_date]

    log.info(f'Summary for {day}/{month}/{year}:')
    for curr_stock_ratio in ratio_params:
        for curr_bid_ratio in bid_ratios:
            for batch_index in range(trade_batches):
                income = today_income[curr_stock_ratio][curr_bid_ratio][batch_index]
                expenses = today_expenses[curr_stock_ratio][curr_bid_ratio][batch_index]
            log.info(f'{curr_stock_ratio},{curr_bid_ratio},{batch_index}: income: {income},'
                  f' expenses {expenses}, '
                  f'daily total: {income - expenses}')

    return today_income, today_expenses, all_today_trade


def filter_snp_symbols(data, symbols):
    return data[data.UnderlyingSymbol.isin(symbols)].copy()


def filter_equity_snp_symbols(data, symbols):
    return data[data.symbol.isin(symbols)].copy()


def plot_results(daily_results, ratio_params, bid_ratios, max_batch, start_time, results_dir):
    dates = sorted(daily_results.keys())
    status_by_bid = dict()
    for curr_bid in bid_ratios:
        status_by_bid[curr_bid] = dict()
    for curr_stock_ratio in ratio_params:
        for curr_bid in bid_ratios:
            status_by_bid[curr_bid][curr_stock_ratio] = dict()
            for batch_index in range(max_batch):
                plot_y = []
                for curr_date in dates:
                    plot_y.append(daily_results[curr_date][curr_stock_ratio][curr_bid][batch_index])
                status_by_bid[curr_bid][curr_stock_ratio][batch_index] = plot_y
    chart_index = 0
    for curr_bid in status_by_bid:
        for batch_index in range(max_batch):
            chart_index += 2
            ratio_index = 0
            for curr_stock_ratio in ratio_params:
                ratio_index += 1
                if ratio_index % 2 == 1:
                    plt.figure(chart_index)
                else:
                    plt.figure(chart_index - 1)
                plt.plot(dates, status_by_bid[curr_bid][curr_stock_ratio][batch_index],
                         label=f'Stock Ratio={curr_stock_ratio}')
            for curr_chart in [chart_index - 1, chart_index]:
                plt.figure(curr_chart)
                plt.legend(loc='upper left')
                plt.xlabel('Date')
                plt.ylabel('Profit / Loss (USD)')
                plt.title(f'Bid Ratio = {curr_bid}, Batch = {batch_index + 1}')
                if curr_chart % 2 == 0:
                    even = '_even'
                else:
                    even = ''
                figure_path = os.path.join(results_dir, f'Bid_{curr_bid}_Batch_{batch_index + 1}{even}.png')
                plt.savefig(figure_path)
                log.info(f'Saved {figure_path}')


def filter_tradable_options(data, trade_date, min_days, max_days, maximum_iv):
    data = data[data.Expiration >= trade_date + datetime.timedelta(days=min_days)]
    data = data[data.Expiration <= trade_date + datetime.timedelta(days=max_days)]
    data = data[data.Volume > 0]
    data = data[data.Bid > 0]
    data = data[data.IV < maximum_iv]
    return data

if __name__ == '__main__':
    run_time = datetime.datetime.now()
    time_text = f'{run_time.year}_{run_time.month:02}_{run_time.day:02}_{run_time.hour:02}_{run_time.minute:02}_' \
                f'{run_time.second}'
    results_dir = f'Results_{time_text}'
    os.makedirs(results_dir)

    # set logger
    formatter = logging.Formatter('%(asctime)s %(levelname)s %(filename)s(%(lineno)d) %(funcName)s %(threadName)s'
                                  ' %(thread)d %(message)s')
    logging.basicConfig(format='%(asctime)s %(levelname)s %(filename)s(%(lineno)d) %(funcName)s %(threadName)s'
                               ' %(thread)d %(message)s')
    log = logging.getLogger('SimulateTrade')
    log.setLevel(logging.DEBUG)
    fh = logging.FileHandler(os.path.join(results_dir, f'Simulate_{time_text}.log'))
    fh.setFormatter(formatter)
    fh.setLevel(logging.DEBUG)
    log.addHandler(fh)

    log.info("Starting")
    start_time = time.time()
    snp_500_symbols = get_snp_symbols(SNP_SYMBOLS_FILE_PATH)

    start_date = date(1950, 1, 1) 
    end_date = date.today()
    is_compressed = False

    # read command line arguments
    if len(sys.argv) > 1:
        start_date =  parse_date(sys.argv[1])
        log.info(f'Set simulation start date to: {start_date}')
    if len(sys.argv) > 2:
        end_date = parse_date(sys.argv[2])
        log.info(f'Set simulation end date to: {end_date}')
    if len(sys.argv) > 3:
        is_compressed = bool(sys.argv[3].lower())

    src_dir = ".\\FilteredCSVs_ziped" if is_compressed else ".\\FilteredCSVs"

    process_source_dir(src_dir, snp_500_symbols, is_compressed, results_dir, start_date, end_date)

    end_time = time.time()
    log.info("Processing took %s seconds", str(end_time - start_time))
