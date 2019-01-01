import SimulateTrade
import pandas as pd
import time
import os
import zipfile
import datetime

if __name__ == '__main__':
    start_time = time.time()
    snp_500_symbols = SimulateTrade.get_snp_symbols(SimulateTrade.SNP_SYMBOLS_FILE_PATH)
    zip_files = SimulateTrade.get_zip_files_in_folder(SimulateTrade.SOURCE_DIR)
    for curr_file in zip_files:
        files_by_zip = {}
        file_path = os.path.join(SimulateTrade.SOURCE_DIR, curr_file)
        files_by_zip[file_path] = SimulateTrade.get_files_from_zip_by_date(file_path)
        for zip_file in files_by_zip:
            print(f'Filtering {zip_file}')
            zip_file_obj = zipfile.ZipFile(zip_file)
            for curr_date in files_by_zip[zip_file]:
                file_time = time.time()
                date_info = files_by_zip[zip_file][curr_date]
                options_file = date_info['options']
                options_data = pd.read_csv(zip_file_obj.open(options_file))
                snp_options = SimulateTrade.filter_snp_symbols(options_data, snp_500_symbols)
                snp_options['Expiration'] = pd.to_datetime(snp_options['Expiration'], format='%m/%d/%Y')
                day = date_info['day']
                month = date_info['month']
                year = date_info['year']
                zip_date = datetime.datetime(year=year, month=month, day=day)
                snp_options = SimulateTrade.filter_tradable_options(snp_options, zip_date, 0, 8, 4)
                snp_options.to_csv(os.path.join(".\\FilteredCSVs", f'options_{year}{month:02}{day:02}.csv'),
                                   index=False)
                print(f'Filtering {zip_file}\\{options_file} took {time.time() - file_time} seconds')
    end_time = time.time()
    print("Processing took", end_time - start_time, "seconds")