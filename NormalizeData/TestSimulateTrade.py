import SimulateTrade
import pandas as pd
import datetime
import zipfile

today = datetime.datetime.today()
test_data = {'UnderlyingSymbol': ['FB', 'AA', 'AMZN', 'GOOG'], 'UnderlyingPrice': [100, 200, 2.3, 1],
             'Expiration': [today, today + datetime.timedelta(days=1), today + datetime.timedelta(days=10),
                            today + datetime.timedelta(days=5)],
             'Bid': [0, 0.1, 0.7, 1.2]}

def test_filter_snp_500():
    df = pd.DataFrame.from_dict(test_data)
    filter_symbols = ['FB', 'AMZN', 'GOOG']
    filtered_df = SimulateTrade.filter_snp_symbols(df, filter_symbols)
    success = True
    for index, row in filtered_df.iterrows():
        if row['UnderlyingSymbol'] not in filter_symbols:
            success = False
            print(f'Filtering error: {row["UnderlyingSymbol"]}')
            break
    return success


def test_tradable_options():
    pass


def test_get_options_by_iv(test_files):
    snp_500_symbols = SimulateTrade.get_snp_symbols(SimulateTrade.SNP_SYMBOLS_FILE_PATH)
    tradeable_symbols = dict()
    for zip_file in test_files:
        for csv_file in test_files[zip_file]:
            zip_file_obj = zipfile.ZipFile(zip_file)
            options_data = pd.read_csv(zip_file_obj.open(csv_file))
            options_data = options_data[options_data.UnderlyingSymbol.isin(snp_500_symbols)].copy()
            options_data['Expiration'] = pd.to_datetime(options_data['Expiration'], format='%m/%d/%Y')
            _, year, month, day = SimulateTrade.parse_filename(csv_file)
            trade_date = datetime.datetime(year=year, month=month, day=day)
            options_data = SimulateTrade.filter_tradable_options(options_data, trade_date, 1, 8, 4)
            trade_options = SimulateTrade.get_options_by_iv(options_data)
            print(f'Done calculating tradable options with the biggest IV on {day}/{month}/{year}')
            trade_date_str = f'{year}_{month}_{day}'
            tradeable_symbols[trade_date_str] = []
            for (trade_group, curr_iv) in trade_options.iteritems():
                tradeable_symbols[trade_date_str].append({trade_group[0]: curr_iv})
    print(tradeable_symbols)

if __name__ == '__main__':
    """result = test_filter_snp_500()
    if result:
        print("Success")
    else:
        print("Error")"""
    test_get_options_by_iv(
        {'.\\Source\\2013_November.zip': ['options_20131101.csv', 'options_20131108.csv', 'options_20131129.csv'],
         '.\\Source\\2014_January.zip': ['options_20140102.csv', 'options_20140115.csv', 'options_20140129.csv']})


