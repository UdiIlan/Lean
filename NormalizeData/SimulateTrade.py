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
HOLD_DAYS = 5


def process_source_dir(source_dir, snp_symbols):
    files_by_zip = {}
    zip_files = get_files_in_folder(source_dir)
    curr_trade = {}
    written_options = []
    total_profit = 0
    open_positions_income = 0
    for curr_file in zip_files:
        file_path = os.path.join(source_dir, curr_file)
        files_by_zip[file_path] = get_files_from_zip_by_date(file_path)

    day_index = 0
    for zip_file in files_by_zip:
        print(f'Processing {zip_file}')
        zip_file_obj = zipfile.ZipFile(zip_file)
        for curr_date in files_by_zip[zip_file]:
            day_index += 1
            if day_index > DAYS_TO_PROCESS:
                break
            date_info = files_by_zip[zip_file][curr_date]
            options_file = date_info['options']
            options_start = time.time()
            options_data = pd.read_csv(zip_file_obj.open(options_file))
            close_positions = dict()
            if len(written_options) >= HOLD_DAYS:
                close_positions = written_options[0]['options']
                open_positions_income -= written_options.pop(0)['income']
            (today_income, today_expenses, curr_trade) = process_options_file(options_data, date_info['year'],
                                                                              date_info['month'], date_info['day'],
                                                                              snp_symbols, close_positions)
            written_options.append({'income': today_income, 'options': curr_trade})
            open_positions_income += today_income
            total_profit += (today_income - today_expenses)
            print(f'Processing options took {time.time() - options_start} seconds, today\'s profit is '
                  f'{today_income - today_expenses}, total profit after {day_index} days is '
                  f'{total_profit - open_positions_income}')

    # Reduce remaining open positions
    total_profit -= open_positions_income
    print(f'Total profit: {total_profit}')


def get_snp_symbols(snp_500_filename):
    # snp_500_filename = "./snp500.txt"
    snp_set = set()
    with open(snp_500_filename) as snp_file:
        for symbol in snp_file:
            snp_set.add(symbol.strip())
    return snp_set


def get_zip_files(zip_path):
    result = []
    with zipfile.ZipFile(zip_path, "r") as f:
        for name in f.namelist():
            result.append(name)
    return result


def get_files_in_folder(folder_path):
    result = []
    for filename in os.listdir(folder_path):
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


def process_options_file(options_data, year, month, day, snp_symbols, last_trade):
    print(f'Handling options for {day}/{month}/{year}')
    today_income = 0
    today_expenses = 0

    # When option expires pay the difference Strike and StockPrice
    for curr_traded_symbol in last_trade:
        symbol_row = options_data[options_data.OptionSymbol == curr_traded_symbol]
        if symbol_row.shape[0] == 0:
            print('Missing symbol: {symbol_row}')
        else:
            buy_price = symbol_row['Ask'].iloc[0]
            spread = buy_price - symbol_row['Bid'].iloc[0]
            today_expenses += buy_price * last_trade[curr_traded_symbol]['size']
            print(f'Buying back {last_trade[curr_traded_symbol]["size"]} * {curr_traded_symbol} for '
                  f'{buy_price}, spread is {spread}, relative spread is {spread / buy_price}')

    snp_options = options_data[options_data.UnderlyingSymbol.isin(snp_symbols)]
    snp_options['Expiration'] = pd.to_datetime(snp_options['Expiration'], format='%m/%d/%Y')
    snp_options = snp_options[snp_options.Volume > 0]
    snp_options = snp_options[snp_options.IV < 4]
    symbol_grouped_options = snp_options.groupby('UnderlyingSymbol')
    max_iv_symbol_options = symbol_grouped_options['IV'].max()  # TODO: find options with strike closest to stock price, get the IV (avg call and put)
    max_iv_symbol_options = max_iv_symbol_options.iloc[np.lexsort([max_iv_symbol_options.index,
                                                                   -1 * max_iv_symbol_options.values])]
    trade_index = 0
    all_today_trade = {}
    for (trade_symbol, curr_iv) in max_iv_symbol_options.iteritems():
        trade_index += 1
        if trade_index > DAILY_TRADE_OPTIONS:
            break
        # print(f'Trading option for {trade_symbol}')
        symbol_option_chain = snp_options[snp_options.UnderlyingSymbol == trade_symbol]

        # Filtering only out of market options that are actually traded
        oom_symbol_options = symbol_option_chain[symbol_option_chain.Volume > 0]
        oom_symbol_options = oom_symbol_options[oom_symbol_options.Bid > 0]
        zip_date = datetime.datetime(year=year, month=month, day=day)
        oom_symbol_options = oom_symbol_options[oom_symbol_options.Expiration > zip_date + datetime.timedelta(days=3)]  # TODO: set to 1
        calls = oom_symbol_options[oom_symbol_options.Type == 'call']
        puts = oom_symbol_options[oom_symbol_options.Type == 'put']
        calls = calls[calls.Bid + calls.Strike > calls.UnderlyingPrice]
        puts = puts[puts.Bid + puts.Strike < puts.UnderlyingPrice]
        if calls.shape[0] == 0 and puts.shape[0] == 0:
            print(f'No tradable options for symbol {trade_symbol}')
        else:
            calls.sort_values(by='IV', inplace=True, ascending=False)  # TODO - remove
            puts.sort_values(by='IV', inplace=True, ascending=False)

            # TODO: add percentage parameter: minimum price (50 cent?), K
            #       let K = 10%. Start from strike. If stock price increases by K then the option breaks even.
            #       Example: stock = 100$, at minimal expiration, call_100=8$, call_105=4$, call_108=3$, call_110=1$
            #                At 10% increase call_100 loses 2$, call_105 loses 1$, call_108,110 earn 1$.
            #                Call 108 will be selected because it's the first that breaks even.
            #       For calls: first option (by strike) where Strike + OptionPrice > StockPrice * (1 + K)
            #       For Puts: first option (by strike) where Strike - OptionPrice < StockPrice * (1 - K)
            #       For pricing take bid
            calls.sort_values(by='Expiration', inplace=True, ascending=False)
            puts.sort_values(by='Expiration', inplace=True, ascending=False)
            option_trade_symbols = [{'symbol': calls['OptionSymbol'].iloc[0],
                                     'price': calls['Bid'].iloc[0]},
                                    {'symbol': puts['OptionSymbol'].iloc[0],
                                     'price': puts['Bid'].iloc[0]}]
            for curr_option_trade_symbol in option_trade_symbols:
                trade_size = int(math.floor((TRADE_PER_SYMBOL / len(option_trade_symbols)) /
                                            curr_option_trade_symbol['price']))
                today_trade = {'price': curr_option_trade_symbol['price'],
                               'size': trade_size}
                print(f'Writing {trade_size} * {curr_option_trade_symbol["symbol"]} for '
                      f'{curr_option_trade_symbol["price"]}')
                today_income += today_trade['price'] * today_trade['size']
                all_today_trade[curr_option_trade_symbol['symbol']] = today_trade
    print(f'Summary for {day}/{month}/{year}: income: {today_income}, expenses {today_expenses}, '
          f'daily total: {today_income - today_expenses}')
    return today_income, today_expenses, all_today_trade


if __name__ == '__main__':
    start_time = time.time()
    snp_500_symbols = get_snp_symbols(SNP_SYMBOLS_FILE_PATH)
    process_source_dir(SOURCE_DIR, snp_500_symbols)
    end_time = time.time()
    print("Processing took", end_time - start_time, "seconds")
