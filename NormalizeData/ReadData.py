"""
    Algorithm description:
    for lines in stockquotes file:
	if symbol is in expanded s&p 500 then create equity file:
		Data\equity\usa\minute\amzn\20131101_trade.zip:
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

DEST_DIR = "C:\\Users\\Udi Ilan\\Documents\\Projects\\ConvertData\\Result"
SOURCE_DIR = 'C:\\Users\\Udi Ilan\\Documents\\Projects\\ConvertData\\Data'
DAILY_TRADE_MINUTE_TIMESTAMP = 55440000


def process_source_dir(source_dir, dest_dir):
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
            process_stocks_file(stock_quotes_data, date_info['year'], date_info['month'], date_info['day'],
                                dest_dir)
            options_file = date_info['options']
            options_data = pandas.read_csv(zip_file_obj.open(options_file))
            process_options_file(options_data, date_info['year'], date_info['month'], date_info['day'],
                                dest_dir)
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


def process_stocks_file(stocks_data, year, month, day, dest_folder):
    for index, row in stocks_data.iterrows():
        symbol = row['symbol']
        open_price = row['open']
        high_price = row['high']
        low_price = row['low']
        close_price = row['close']
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
            stockquote_filename = f'{year}{month:02}{day:02}_{symbol}_minute_trade.csv'
            zip_file_handle.writestr(stockquote_filename,
                                     f'{DAILY_TRADE_MINUTE_TIMESTAMP},{open_price},{high_price},{low_price},'
                                     f'{close_price},{volume}')
            zip_file_handle.close()

def process_options_file(stocks_data, year, month, day, dest_folder):



if __name__ == '__main__':
    process_source_dir(SOURCE_DIR, DEST_DIR)

