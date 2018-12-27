"""
    Algorithm description:
    for lines in stockquotes file:
	if symbol is in expanded s&p 500 then create equity file:
		Data\\equity\\usa\\minute\\amzn\\20131101_trade.zip:
			20131101_amzn_minute_trade.csv
			55440000,3590020,3590020,3590020,3590020,16
			{permanent timestamp}, 4 * {symbol price}, symbol volume

    for lines in date file:
        if symbol is in expanded s&p 500:
            for each option:
                create 3 csv files:
                    20131101_amzn_minute_openinterest_american_call_360000_20131105.csv:
                        23460000,505
                        {permanent timestamp}, {open interest}
                    20131101_amzn_minute_quote_american_call_3600000_20131105.csv:
                        55440000,39000,39000,39000,39000,650,41500,41500,41500,41500,650
                        {permanent timestamp}, 4 * bid, volume / 2, 4 * ask, volume / 2
                    20131101_amzn_minute_trade_american_call_3600000_20131105.csv:
                        55440000,645000,645000,645000,645000,2
                        {permanent timestamp}, 4 * last, volume
"""

import pandas
import zipfile
import re
import os
from datetime import datetime
import time

DEST_DIR = ".\\Destination"
SOURCE_DIR = '.\\Source'
SNP_SYMBOLS_FILE_PATH = ".\\snp500.txt"
DAILY_TRADE_MINUTE_TIMESTAMP = 55440000


def process_source_dir(source_dir, dest_dir, snp_symbols):
    files_by_zip = {}
    zip_files = get_files_in_folder(source_dir)
    for curr_file in zip_files:
        file_path = os.path.join(source_dir, curr_file)
        files_by_zip[file_path] = get_files_from_zip_by_date(file_path)

    for zip_file in files_by_zip:
        zip_file_obj = zipfile.ZipFile(zip_file)
        for curr_date in files_by_zip[zip_file]:
            date_info = files_by_zip[zip_file][curr_date]
            stock_quotes_file = date_info['stockquotes']
            stock_quotes_data = pandas.read_csv(zip_file_obj.open(stock_quotes_file))
            stocks_start = time.time()
            process_stocks_file(stock_quotes_data, date_info['year'], date_info['month'], date_info['day'],
                                dest_dir, snp_symbols)
            stocks_end = time.time()
            print(f'Processing stocks took {stocks_end - stocks_start} seconds')
            options_file = date_info['options']
            options_data = pandas.read_csv(zip_file_obj.open(options_file))
            process_options_file(options_data, date_info['year'], date_info['month'], date_info['day'],
                                dest_dir, snp_symbols)
            print(f'Processing options took {time.time() - stocks_end} seconds')
            break
        break


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
    return result


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


def process_stocks_file(stocks_data, year, month, day, dest_folder, snp_symbols):
    print(f'Handling stocks for {day}/{month}/{year}')
    for index, row in stocks_data.iterrows():
        symbol = row['symbol']
        if symbol in snp_symbols:
            print(f'Handling the stock {symbol} at {day}/{month}/{year}')
            open_price = row['open'] * 10000
            high_price = row['high'] * 10000
            low_price = row['low'] * 10000
            close_price = row['close'] * 10000
            volume = row['volume']
            zip_dir = os.path.join(dest_folder, 'equity', 'usa', 'minute', symbol.lower())
            dir_created = True
            try:
                if not os.path.exists(zip_dir):
                    os.makedirs(zip_dir)
            except Exception as e:
                print("directory exception:", e)
                dir_created = False
            if dir_created:
                zip_path = os.path.join(zip_dir, f'{year}{month:02}{day:02}_trade.zip')
                zip_file_handle = zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED)
                stockquote_filename = f'{year}{month:02}{day:02}_{symbol.lower()}_minute_trade.csv'
                zip_file_handle.writestr(stockquote_filename,
                                         f'{DAILY_TRADE_MINUTE_TIMESTAMP},{open_price},{high_price},{low_price},'
                                         f'{close_price},{volume}')
                zip_file_handle.close()


def process_options_file(options_data, year, month, day, dest_folder, snp_symbols):
    print(f'Handling options for {day}/{month}/{year}')
    file_prefix = f'{year}{month:02}{day:02}'
    format_str = "{}"
    zip_format_string = f'{file_prefix}_{format_str}_american.zip'
    curr_stock_symbol = ''
    open_interest_zip_handle = None
    quote_zip_handle = None
    trade_zip_handle = None
    option_index = 0
    for index, row in options_data.iterrows():
        if option_index > 20:
            break
        stock_symbol = row['UnderlyingSymbol']
        if stock_symbol in snp_symbols:
            if stock_symbol != curr_stock_symbol:
                print(f'Handling the options for {stock_symbol} on {day}/{month}/{year}')
                if open_interest_zip_handle:
                    open_interest_zip_handle.close()
                if quote_zip_handle:
                    quote_zip_handle.close()
                if trade_zip_handle:
                    trade_zip_handle.close()
                output_path = os.path.join(dest_folder, 'option', 'usa', 'minute', stock_symbol.lower())
                dir_created = True
                try:
                    if not os.path.exists(output_path):
                        os.makedirs(output_path)
                except Exception as e:
                    print("directory exception:", e)
                    dir_created = False
                if dir_created:
                    option_index += 1
                    curr_stock_symbol = stock_symbol
                    open_interest_zip_path = os.path.join(output_path, zip_format_string.format("openinterest"))
                    open_interest_zip_handle = zipfile.ZipFile(open_interest_zip_path, 'w', zipfile.ZIP_DEFLATED)
                    quote_zip_path = os.path.join(output_path, zip_format_string.format("quote"))
                    quote_zip_handle = zipfile.ZipFile(quote_zip_path, 'w', zipfile.ZIP_DEFLATED)
                    trade_zip_path = os.path.join(output_path, zip_format_string.format("trade"))
                    trade_zip_handle = zipfile.ZipFile(trade_zip_path, 'w', zipfile.ZIP_DEFLATED)
            if open_interest_zip_handle and quote_zip_handle and trade_zip_handle:
                expiration_date = datetime.strptime(row['Expiration'], "%m/%d/%Y")
                csv_file_template = f'{file_prefix}_{stock_symbol.lower()}_minute_{format_str}_american_' \
                                    f'{row["Type"]}_{int(float(row["Strike"]) * 10000)}_{expiration_date.year}' \
                                    f'{expiration_date.month:02}{expiration_date.day:02}.csv'
                open_interest_row = f'{DAILY_TRADE_MINUTE_TIMESTAMP},{row["OpenInterest"]}'
                open_interest_csv = csv_file_template.format("openinterest")
                #open_interest_zip_handle.writestr(open_interest_csv, open_interest_row)
                option_quote_bid = row['Bid'] * 10000
                option_quote_ask = row['Ask'] * 10000
                option_quote_half_volume = int(row['Volume'] / 2)
                quote_row = f'{DAILY_TRADE_MINUTE_TIMESTAMP},{option_quote_bid},{option_quote_bid},{option_quote_bid}' \
                            f',{option_quote_bid},{option_quote_half_volume},{option_quote_ask},{option_quote_ask},' \
                            f'{option_quote_ask},{option_quote_ask},{option_quote_half_volume}'
                quote_csv = csv_file_template.format("quote")
                #quote_zip_handle.writestr(quote_csv, quote_row)
                option_trade_last = row['Last'] * 10000
                trade_row = f'{DAILY_TRADE_MINUTE_TIMESTAMP},{option_trade_last},{option_trade_last},' \
                            f'{option_trade_last},{option_trade_last},{row["Volume"]}'
                trade_csv = csv_file_template.format("trade")
                #trade_zip_handle.writestr(trade_csv, trade_row)
    if open_interest_zip_handle:
        open_interest_zip_handle.close()
    if quote_zip_handle:
        quote_zip_handle.close()
    if trade_zip_handle:
        trade_zip_handle.close()


if __name__ == '__main__':
    start_time = time.time()
    snp_500_symbols = get_snp_symbols(SNP_SYMBOLS_FILE_PATH)
    process_source_dir(SOURCE_DIR, DEST_DIR, snp_500_symbols)
    end_time = time.time()
    print("Processing took", end_time - start_time, "seconds")
