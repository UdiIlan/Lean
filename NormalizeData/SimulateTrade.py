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
import numpy as np

SOURCE_DIR = '.\\Source'
SNP_SYMBOLS_FILE_PATH = ".\\snp500.txt"
DAILY_TRADE_OPTIONS = 5
PRICE_FACTOR = 1
TRADE_PER_SYMBOL = 1000
DAYS_TO_PROCESS = 100
MINIMUM_BID = 0.5
EXPECTED_STOCK_CHANGE_RATIO = [0, 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09, 0.1]
BID_RATIO = [1, 0.5]


def process_source_dir(source_dir, snp_symbols):
    files_by_zip = {}
    zip_files = get_files_in_folder(source_dir)
    curr_trade = {}
    written_options = []
    total_profit = dict()
    for curr_file in zip_files:
        file_path = os.path.join(source_dir, curr_file)
        files_by_zip[file_path] = get_files_from_zip_by_date(file_path)

    day_index = 0
    open_positions = dict()
    for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
        open_positions[curr_stock_ratio] = dict()
        total_profit[curr_stock_ratio] = dict()
        for curr_bid_ratio in BID_RATIO:
            open_positions[curr_stock_ratio][curr_bid_ratio] = dict()
            total_profit[curr_stock_ratio][curr_bid_ratio] = 0
    for zip_file in files_by_zip:
        print(f'Processing {zip_file}')
        zip_file_obj = zipfile.ZipFile(zip_file)
        for curr_date in files_by_zip[zip_file]:
            day_index += 1
            #if day_index < 5:
            #    continue
            if day_index > DAYS_TO_PROCESS:
                break
            date_info = files_by_zip[zip_file][curr_date]
            options_file = date_info['options']
            options_start = time.time()
            options_data = pd.read_csv(zip_file_obj.open(options_file))
            day = date_info['day']
            month = date_info['month']
            year = date_info['year']
            (today_income, today_expenses, curr_trade) = process_options_file(options_data, year, month, day,
                                                                              snp_symbols, open_positions,
                                                                              EXPECTED_STOCK_CHANGE_RATIO, BID_RATIO)
            for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
                for curr_bid_ratio in BID_RATIO:
                    for expiration_date in curr_trade[curr_stock_ratio][curr_bid_ratio]:
                        if expiration_date not in open_positions[curr_stock_ratio][curr_bid_ratio]:
                            open_positions[curr_stock_ratio][curr_bid_ratio][expiration_date] = \
                                curr_trade[curr_stock_ratio][curr_bid_ratio][expiration_date]
                        else:
                            open_positions[curr_stock_ratio][curr_bid_ratio][expiration_date] += \
                                curr_trade[curr_stock_ratio][curr_bid_ratio][expiration_date]
                    open_positions_cost = calc_positions_cost(open_positions[curr_stock_ratio][curr_bid_ratio])
                    total_profit[curr_stock_ratio][curr_bid_ratio] += \
                        (today_income[curr_stock_ratio][curr_bid_ratio] -
                         today_expenses[curr_stock_ratio][curr_bid_ratio])
                    print(f'{curr_stock_ratio},{curr_bid_ratio}: profit for {day}/{month}/{year}'
                          f' is {today_income[curr_stock_ratio][curr_bid_ratio] - today_expenses[curr_stock_ratio][curr_bid_ratio]}, '
                          f'total profit after {day_index} '
                          f'days is {total_profit[curr_stock_ratio][curr_bid_ratio] - open_positions_cost}')
            print(f'Processing options for {day}/{month}/{year} took {time.time() - options_start} seconds')

    # Reduce remaining open positions
    for curr_stock_ratio in EXPECTED_STOCK_CHANGE_RATIO:
        for curr_bid_ratio in BID_RATIO:
            total_profit[curr_stock_ratio][curr_bid_ratio] -= \
                calc_positions_cost(open_positions[curr_stock_ratio][curr_bid_ratio])
            print(f'{curr_stock_ratio},{curr_bid_ratio} Total profit: '
                  f'{total_profit[curr_stock_ratio][curr_bid_ratio]}')

    print("Total profit", total_profit)


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
    print(zip_path)
    with zipfile.ZipFile(zip_path, "r") as f:
        for name in f.namelist():
            result.append(name)
    return result


def get_files_in_folder(folder_path):
    result = []
    for filename in os.listdir(folder_path):
        if '.zip' in filename:
            result.append(filename)
    result = sorted(result, key=filename_to_sort_number)
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
        m = re.search('(.+)_(\d\d\d\d)(\d\d)(\d\d)\.+', curr_csv)
        file_type = m.group(1)
        year = int(m.group(2))
        month = int(m.group(3))
        day = int(m.group(4))
        date_key = f'{year}_{month}_{day}'
        if file_type in ['stockquotes', 'options']:
            if date_key not in files_in_date:
                files_in_date[date_key] = {'year': year, 'month': month, 'day': day}
            files_in_date[date_key][file_type] = curr_csv
    return files_in_date


def process_options_file(options_data, year, month, day, snp_symbols, current_options, ratio_params, bid_ratios):
    print(f'Handling options for {day}/{month}/{year}')
    zip_date = datetime.datetime(year=year, month=month, day=day)

    snp_options = options_data[options_data.UnderlyingSymbol.isin(snp_symbols)].copy()
    snp_options['Expiration'] = pd.to_datetime(snp_options['Expiration'], format='%m/%d/%Y')

    # Only options that expire a week from today
    snp_options = snp_options[snp_options.Expiration >= zip_date + datetime.timedelta(days=1)]
    snp_options = snp_options[snp_options.Expiration <= zip_date + datetime.timedelta(days=8)]
    snp_options = snp_options[snp_options.Volume > 0]
    snp_options = snp_options[snp_options.Bid > 0]
    snp_options = snp_options[snp_options.IV < 4]
    #snp_options = snp_options.loc[(snp_options.groupby(
    #    ['UnderlyingSymbol', 'Expiration', 'Strike']).filter(lambda x: len(x) > 1)).index]
#    try:
    if 'OptionPriceDifference' not in snp_options:
        snp_options['OptionPriceDifference'] = snp_options.apply(lambda row: abs(row['UnderlyingPrice'] -
                                                                                 row['Strike']), axis=1)
    '''except Exception as e:
        print('Caught exception:', e, "# of rows:", len(snp_options))
        for row_index, row in snp_options.iterrows():
            option_price_difference = abs(row['UnderlyingPrice'] - row['Strike'])
            snp_options.set_value(row_index, 'OptionPriceDifference', option_price_difference)
            print(f'Set row {row_index} to {option_price_difference}') '''

    symbol_grouped_options = snp_options.groupby('UnderlyingSymbol')

    # Filter options with strike closest to strike
    min_option_difference_index = \
        symbol_grouped_options['OptionPriceDifference'].transform(min) >= snp_options['OptionPriceDifference']
    closest_to_strike_options = snp_options[min_option_difference_index].copy()

    # Calculate IV as average between call and put of closest to strike for each expiration
    average_iv_expiration_grouped_closest_to_strike_options = closest_to_strike_options.groupby(
        ['UnderlyingSymbol', 'Expiration'])['IV'].mean()
    average_iv_expiration_grouped_closest_to_strike_options.sort_values(inplace=True, ascending=False)
    #average_iv_expiration_grouped_closest_to_strike_options = \
    #    average_iv_expiration_grouped_closest_to_strike_options.iloc(np.lexsort(
    #        [average_iv_expiration_grouped_closest_to_strike_options.index,
    #         -1 * average_iv_expiration_grouped_closest_to_strike_options.values]))
    #max_iv_symbol_options = symbol_grouped_options['IV'].max()  # TODO: find options with strike closest to stock price, get the IV (avg call and put)
    #max_iv_symbol_options = max_iv_symbol_options.iloc[np.lexsort([max_iv_symbol_options.index,
    #                                                               -1 * max_iv_symbol_options.values])]

    trade_index = 0
    all_today_trade = dict()
    today_income = dict()
    for curr_stock_ratio in ratio_params:
        today_income[curr_stock_ratio] = dict()
        all_today_trade[curr_stock_ratio] = dict()
        for curr_bid_ratio in bid_ratios:
            today_income[curr_stock_ratio][curr_bid_ratio] = 0
            all_today_trade[curr_stock_ratio][curr_bid_ratio] = dict()
    for (trade_group, curr_iv) in average_iv_expiration_grouped_closest_to_strike_options.iteritems():
        if trade_index >= DAILY_TRADE_OPTIONS:
            break
        trade_symbol = trade_group[0]
        #print(f'Trading option for {trade_symbol}')
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
            #print(f'No tradable options for symbol {trade_symbol}')
            pass
        else:
            #       Start from strike. If stock price increases by K then the option breaks even.
            #       Example: stock = 100$, at minimal expiration, call_100=8$, call_105=4$, call_108=3$, call_110=1$
            #                At 10% increase call_100 loses 2$, call_105 loses 1$, call_108,110 earn 1$.
            #                Call 108 will be selected because it's the first that breaks even.
            #       For calls: first option (by strike) where Strike + OptionPrice > StockPrice * (1 + K)
            #       For Puts: first option (by strike) where Strike - OptionPrice < StockPrice * (1 - K)
            #       For pricing take bid
            for curr_stock_ratio in ratio_params:
                for curr_bid_ratio in bid_ratios:
                    calls = calls[calls.Strike + curr_bid_ratio * calls.Bid + (1 - curr_bid_ratio) * calls.Ask >
                                  calls.UnderlyingPrice * (1 + curr_stock_ratio)]
                    puts = puts[puts.Strike - curr_bid_ratio * puts.Bid - (1 - curr_bid_ratio) * puts.Ask <
                                puts.UnderlyingPrice * (1 - curr_stock_ratio)]
                    calls.sort_values(by='Strike', inplace=True, ascending=True)
                    puts.sort_values(by='Strike', inplace=True, ascending=False)
                    if calls.shape[0] == 0 or puts.shape[0] == 0:
                        pass
                        #print(f'No tradable options for symbol {trade_symbol} according to parameter '
                        #      f'{EXPECTED_STOCK_CHANGE_RATIO}')
                    if calls.shape[0] > 0 and puts.shape[0] > 0:
                        trade_index += 1
                        option_trade_symbols = [{'symbol': calls['OptionSymbol'].iloc[0],
                                                 'price': curr_bid_ratio * calls['Bid'].iloc[0] +
                                                          (1 - curr_bid_ratio) * calls['Ask'].iloc[0],
                                                 'type': 'call',
                                                 'expiration': calls['Expiration'].iloc[0],
                                                 'underlying_symbol': calls['UnderlyingSymbol'].iloc[0]},
                                                {'symbol': puts['OptionSymbol'].iloc[0],
                                                 'price': curr_bid_ratio * puts['Bid'].iloc[0] + (
                                                         1 - curr_bid_ratio) * puts['Ask'].iloc[0],
                                                 'type': 'put',
                                                 'expiration': puts['Expiration'].iloc[0],
                                                 'underlying_symbol': puts['UnderlyingSymbol'].iloc[0]}]
                        for curr_option_trade_symbol in option_trade_symbols:
                            trade_size = int(math.floor((TRADE_PER_SYMBOL / len(option_trade_symbols)) /
                                                        curr_option_trade_symbol['price']))
                            curr_option_trade_symbol['size'] = trade_size
                            print(f'{curr_stock_ratio},{curr_bid_ratio}: Writing '
                                  f'{trade_size} * {curr_option_trade_symbol["symbol"]} for '
                                  f'{curr_option_trade_symbol["price"]}')
                            today_income[curr_stock_ratio][curr_bid_ratio] += \
                                curr_option_trade_symbol['price'] * trade_size
                            if curr_option_trade_symbol['expiration'] not in \
                                    all_today_trade[curr_stock_ratio][curr_bid_ratio]:
                                all_today_trade[curr_stock_ratio][curr_bid_ratio][curr_option_trade_symbol['expiration']] = []
                            all_today_trade[curr_stock_ratio][curr_bid_ratio][curr_option_trade_symbol['expiration']].append(
                                curr_option_trade_symbol)

    # When option expires pay the difference Strike and StockPrice
    today_expenses = dict()
    for curr_stock_ratio in ratio_params:
        today_expenses[curr_stock_ratio] = dict()
        for curr_bid_ratio in bid_ratios:
            today_expenses[curr_stock_ratio][curr_bid_ratio] = 0
            if zip_date in current_options[curr_stock_ratio][curr_bid_ratio]:
                for curr_traded_symbol in current_options[curr_stock_ratio][curr_bid_ratio][zip_date]:
                    symbol_row = options_data[options_data.OptionSymbol == curr_traded_symbol['symbol']]
                    if symbol_row.shape[0] == 0:
                        print('Missing symbol: {symbol_row}')
                    else:
                        underlying_price = symbol_row['UnderlyingPrice'].iloc[0]
                        strike_price = symbol_row['Strike'].iloc[0]
                        pay_per_option = 0
                        if curr_traded_symbol['type'] == 'call' and underlying_price > strike_price:
                            pay_per_option = underlying_price - strike_price
                        elif curr_traded_symbol['type'] == 'put' and underlying_price < strike_price:
                            pay_per_option = strike_price - underlying_price
                        symbol_expenses = pay_per_option * curr_traded_symbol['size']
                        today_expenses[curr_stock_ratio][curr_bid_ratio] += symbol_expenses
                        print(f'{curr_stock_ratio},{curr_bid_ratio}: For {curr_traded_symbol["symbol"]} the underlying price is'
                              f' {underlying_price}, paying {pay_per_option} for {curr_traded_symbol["size"]} '
                              f'options, total to pay is {symbol_expenses}')
                del current_options[curr_stock_ratio][curr_bid_ratio][zip_date]
    print(f'Summary for {day}/{month}/{year}:')
    for curr_stock_ratio in ratio_params:
        for curr_bid_ratio in bid_ratios:
            print(f'{curr_stock_ratio},{curr_bid_ratio}: income: {today_income[curr_stock_ratio][curr_bid_ratio]},'
                  f' expenses {today_expenses[curr_stock_ratio][curr_bid_ratio]}, '
                  f'daily total: {today_income[curr_stock_ratio][curr_bid_ratio] - today_expenses[curr_stock_ratio][curr_bid_ratio]}')

    return today_income, today_expenses, all_today_trade


if __name__ == '__main__':
    start_time = time.time()
    snp_500_symbols = get_snp_symbols(SNP_SYMBOLS_FILE_PATH)
    process_source_dir(SOURCE_DIR, snp_500_symbols)
    end_time = time.time()
    print("Processing took", end_time - start_time, "seconds")
