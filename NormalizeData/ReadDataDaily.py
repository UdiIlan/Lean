"""
    Algorithm description:
    for lines in stockquotes file:
	if symbol is in expanded s&p 500 then create equity file:
		Data\\equity\\usa\\daily\\amzn.zip:
			20131101_amzn_minute_trade.csv
			55440000,3590020,3590020,3590020,3590020,16
			{permanent timestamp}, 4 * {symbol price}, symbol volume

    for lines in date file:
        if symbol is in expanded s&p 500:
            for each option:
                create 3 zip files:
                    amzn_openinterest_american.zip:
                        23460000,505
                        {permanent timestamp}, {open interest}
                    amzn_quote_american.zip:
                        55440000,39000,39000,39000,39000,650,41500,41500,41500,41500,650
                        {permanent timestamp}, 4 * bid, volume / 2, 4 * ask, volume / 2
                    amzn_trade_american.zip:
                        55440000,645000,645000,645000,645000,2
                        {permanent timestamp}, 4 * last, volume
"""

import pandas
import zipfile
import re
import os
from datetime import datetime
import time
import uuid

DEST_DIR = ".\\Destination"
SOURCE_DIR = '.\\Source'
SNP_SYMBOLS_FILE_PATH = ".\\snp500.txt"


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
    
    print('archiving output...')
    equity_out_dir = os.path.join(dest_dir, 'equity', 'usa', 'daily')
    archive_dir_files(equity_out_dir)
    options_out_dir = os.path.join(dest_dir, 'option', 'usa', 'daily')
    archive_dir_folders(options_out_dir)

def get_snp_symbols(snp_500_filename):
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
    out_dir = os.path.join(dest_folder, 'equity', 'usa', 'daily')
    ensure_dir_exist(out_dir)

    for index, row in stocks_data.iterrows():
        symbol = row['symbol']
        if symbol in snp_symbols:
            print(f'Handling the stock {symbol} at {day}/{month}/{year}')
            open_price = row['open'] * 10000
            high_price = row['high'] * 10000
            low_price = row['low'] * 10000
            close_price = row['close'] * 10000
            volume = row['volume']
            stockquote_filename = os.path.join(out_dir, f'{symbol.lower()}.csv')
            cur_date = f'{year}{month:02}{day:02} 00:00'
            stock_row = f'{cur_date},{open_price},{high_price},{low_price},{close_price},{volume}\n'
            with open(stockquote_filename, "a") as stock_csv_file:
                stock_csv_file.write(stock_row)

def ensure_dir_exist(dir_path):
     if not os.path.exists(dir_path):
        os.makedirs(dir_path)


def process_options_file(options_data, year, month, day, dest_folder, snp_symbols):
    print(f'Handling options for {day}/{month}/{year}')
    cur_date = f'{year}{month:02}{day:02} 00:00'
    format_str = "{}"
    curr_stock_symbol = ''
    option_index = 0
    output_path = os.path.join(dest_folder, 'option', 'usa', 'daily')
    ensure_dir_exist(output_path)

    for index, row in options_data.iterrows():
        if option_index > 50:
            break
        stock_symbol = row['UnderlyingSymbol']
        dir_format_path = f'{stock_symbol.lower()}_{format_str}_american'
        if stock_symbol in snp_symbols:
            if stock_symbol != curr_stock_symbol:
                print(f'Handling the options for {stock_symbol} on {day}/{month}/{year}')
                open_interest_dir = os.path.join(output_path, dir_format_path.format("openinterest"))
                quote_dir = os.path.join(output_path, dir_format_path.format("quote"))
                trade_dir = os.path.join(output_path, dir_format_path.format("trade"))
                ensure_dir_exist(open_interest_dir)
                ensure_dir_exist(quote_dir)
                ensure_dir_exist(trade_dir)

                option_index += 1
                curr_stock_symbol = stock_symbol

            # if open_interest_zip_handle and quote_zip_handle and trade_zip_handle:
            expiration_date = datetime.strptime(row['Expiration'], "%m/%d/%Y")
            csv_file_template = f'{stock_symbol.lower()}_{format_str}_american_' \
                                f'{row["Type"]}_{int(float(row["Strike"]) * 10000)}_{expiration_date.year}' \
                                f'{expiration_date.month:02}{expiration_date.day:02}.csv'
            open_interest_row = f'{cur_date},{row["OpenInterest"]}\n'
            open_interest_csv = os.path.join(open_interest_dir, csv_file_template.format("openinterest"))
            with open(open_interest_csv, "a") as open_interest_csv_file:
                open_interest_csv_file.write(open_interest_row)

            option_quote_bid = row['Bid'] * 10000
            option_quote_ask = row['Ask'] * 10000
            option_quote_half_volume = int(row['Volume'] / 2)
            quote_row = f'{cur_date},{option_quote_bid},{option_quote_bid},{option_quote_bid}' \
                        f',{option_quote_bid},{option_quote_half_volume},{option_quote_ask},{option_quote_ask},' \
                        f'{option_quote_ask},{option_quote_ask},{option_quote_half_volume}\n'
            quote_csv =  os.path.join(quote_dir, csv_file_template.format("quote"))
            with open(quote_csv, "a") as quote_csv_file:
                quote_csv_file.write(quote_row)

            option_trade_last = row['Last'] * 10000
            trade_row = f'{cur_date},{option_trade_last},{option_trade_last},' \
                        f'{option_trade_last},{option_trade_last},{row["Volume"]}\n'
            trade_csv = os.path.join(trade_dir, csv_file_template.format("trade"))
            with open(trade_csv, "a") as trade_csv_file:
                trade_csv_file.write(trade_row)

def archive_dir_files(path):
    for root, dirs, files in os.walk(path):
        for file in files:
            archive(os.path.join(root, file))

def archive_dir_folders(path):
    for root, dirs, files in os.walk(path):
        for dr in dirs:
            archive(os.path.join(root, dr))


def archive(path):
    zip_file_name=f'{os.path.splitext(path)[0]}.zip'
    # print(f'zip file name: {zip_file_name}')
    zip_file_handle = zipfile.ZipFile(zip_file_name, 'a', zipfile.ZIP_DEFLATED)

    if os.path.isdir(path):
        for root, dirs, files in os.walk(path):
            for file in files:
                file_path = os.path.join(root, file)
                zip_file_handle.write(file_path, arcname=os.path.basename(file))
                os.remove(file_path)
        os.rmdir(path)
    else:
        zip_file_handle.write(path, arcname=os.path.basename(path))
        os.remove(path)

    zip_file_handle.close()


if __name__ == '__main__':
    start_time = time.time()
    snp_500_symbols = get_snp_symbols(SNP_SYMBOLS_FILE_PATH)
    process_source_dir(SOURCE_DIR, DEST_DIR, snp_500_symbols)
    end_time = time.time()
    print("Processing took", end_time - start_time, "seconds")
