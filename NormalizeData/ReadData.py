import pandas
import zipfile
import re
import os

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
    symbol = 'GOOG'
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
            zip_file_handle.writestr(f'{year}{month:02}{day:02}_{symbol.lower()}_minute_trade.csv',
                                     f'5555555,{open_price},{high_price},{low_price},{close_price},{volume}')
            zip_file_handle.close()

dest_dir = "."
source_dir = 'C:\\Users\\Udi Ilan\\Documents\\Projects\\ConvertData\\Data'
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
                            "C:\\Users\\Udi Ilan\\Documents\\Projects\\ConvertData\\Result")
        options_file = stock_quotes_file = date_info['options']
        options_data = pandas.read_csv(zip_file_obj.open(options_file))
        break
    break



zf = zipfile.ZipFile('C:/Users/udiil/Downloads/2013_November.zip')
year = 2013
day = 1
month = 11
df = pandas.read_csv(zf.open('stockquotes_20131101.csv'))
symbol_index = df.columns.get_loc('symbol')
open_index = df.columns.get_loc('open')
high_index = df.columns.get_loc('high')
low_index = df.columns.get_loc('low')
close_index = df.columns.get_loc('close')
volume_index = df.columns.get_loc('volume')
DAILY_TRADE_MINUTE_TIMESTAMP = 55440000

for index, row in df.iterrows():
    symbol = row[symbol_index]
    if symbol in snp_set:
        open_price = row[open_index]
        high = row[high_index]
        low = row[low_index]
        close = row[close_index]
        volume = row[volume_index]
        new_row = [DAILY_TRADE_MINUTE_TIMESTAMP, open_price, high, low, close, volume]
        stockquote_filename = f'{year}{month:02}{day:02}_{symbol}_minute_trade.csv'
        print("filename", stockquote_filename)
        csv_writer(new_row, stockquote_filename)
        break