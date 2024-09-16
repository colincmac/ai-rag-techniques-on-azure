# Extract 13F-HR data

from datetime import datetime
from typing import List, Dict
import numpy as np
import pandas as pd
import xmltodict
from glob import glob
FILING_MANAGER_ADDRESS_COL = 'managerAddress'
FILING_MANAGER_NAME_COL = 'managerName'
FILING_MANAGER_CIK_COL = 'managerCik'
REPORT_PERIOD_COL = 'reportCalendarOrQuarter'
COMPANY_CUSIP_COL = 'cusip'
COMPANY_CUSIP6_COL = 'cusip6'
COMPANY_NAME_COL = 'companyName'
SOURCE_ID_COL = 'source'
VALUE_COL = 'value'
SHARES_COL = 'shares'
class Form13F_HR_Extractor:
    def __init__(self) -> None:
        pass
    
    def process_directory_files(self, input_directory: str, output_csv_file: str, top_n_periods: int = None):
        files = glob(f"{input_directory}/*/*")
        filings_df, failures = self.parse_from_dir(files)
        stg_df = self.aggregate_data(filings_df)
        if top_n_periods is not None:
            stg_df = self.filter_data(stg_df, top_n_periods)
        stg_df.to_csv(output_csv_file, index=False)
        print(f'===== Processed {len(stg_df)} files ====')

        print(f'===== Had {len(failures)} failed file parsings ====')
        for failure in failures:
            print(failure)
        return 0

    # function to strip namespaces post xmltodict transformation
    def strip_ns(self, x):
        if isinstance(x, dict):
            x_striped = dict()
            for k, v in x.items():
                x_striped[k.split(':')[-1]] = self.strip_ns(v)
        elif isinstance(x, list):
            x_striped = [self.strip_ns(i) for i in x]
        else:
            x_striped = x
        return x_striped


    def extract_submission_info(self, contents: list[str]) -> str:
        namespaces = {
            'http://www.sec.gov/edgar/common/': None, # skip this namespace
        }
        xml = contents[1].split('</XML>')[0].strip()
        return self.strip_ns(xmltodict.parse(xml, process_namespaces=True, namespaces=namespaces))['edgarSubmission']
    
    def extract_file_id(self, contents: str) -> str:
            namespaces = {
                'http://www.sec.gov/edgar/common/': None, # skip this namespace
            }
            xml = contents[1].split('</XML>')[0].strip()
            return self.strip_ns(xmltodict.parse(xml, process_namespaces=True, namespaces=namespaces))['edgarSubmission']



    def extract_investment_info(self, contents: str) -> str:
        xml = contents[2].split('</XML>')[0].strip()
        return self.strip_ns(xmltodict.parse(xml))['informationTable']['infoTable']


    def estimate_cusip6(self, cusip: str) -> str:
        # Padding of 3 zeros is suspect - likely has a padded zero. This is inconsistent among form13 filers
        if cusip.startswith('000'):
            return cusip.upper()[1:7]
        return cusip.upper()[:6]


    def filter_and_format(self, info_tables: str, manager_address: str, manager_cik: str, manager_name: str,
                          report_period: datetime.date) -> List[Dict]:
        res = []
        if isinstance(info_tables, dict):
            info_tables = [info_tables]
        for info_table in info_tables:
            # Skip none to report incidences
            if info_table['cusip'] == '000000000':
                pass
            # Only want stock holdings, not options
            if info_table['shrsOrPrnAmt']['sshPrnamtType'] != 'SH':
                pass
            # Only want holdings over $10m
            # elif (float(info_table['value']) * 1000) < 10000000:
            #     pass
            # Only want common stock
            # elif info_table['titleOfClass'] != 'COM':
            #     pass
            elif "COM" not in info_table['titleOfClass'] and "CL" not in info_table['titleOfClass'] and "ORD" not in info_table['titleOfClass'] and "SHS" not in info_table["titleOfClass"] and "STOCK" not in info_table["titleOfClass"]:
                #print("not common stock________", info_table['titleOfClass'], "___________",info_table['nameOfIssuer'])
                pass
            else:
                res.append({
                    FILING_MANAGER_CIK_COL: manager_cik,
                    FILING_MANAGER_NAME_COL: manager_name,
                    FILING_MANAGER_ADDRESS_COL: manager_address,
                    REPORT_PERIOD_COL: report_period,
                    COMPANY_CUSIP_COL: info_table['cusip'].upper(),
                    COMPANY_CUSIP6_COL: self.estimate_cusip6(info_table['cusip']),
                    COMPANY_NAME_COL: info_table['nameOfIssuer'],
                    VALUE_COL: info_table['value'].replace(' ', '') + '000',
                    SHARES_COL: info_table['shrsOrPrnAmt']['sshPrnamt']})
        return res


    def extract_dicts(self, txt: str) -> List[Dict]:
        contents = txt.split('<XML>')
        submt_dict = self.extract_submission_info(contents)
        mng_cik = submt_dict['headerData']['filerInfo']['filer']['credentials']['cik']
        mng_name = submt_dict['formData']['coverPage']['filingManager']['name']
        try:
            mng_address = ", ".join(list(submt_dict['formData']['coverPage']['filingManager']['address'].values()))
        except:
            print(submt_dict['formData']['coverPage']['filingManager']['address'])
            exit()
        report_period = submt_dict['formData']['coverPage']['reportCalendarOrQuarter']
        info_dict = self.extract_investment_info(contents)
        return self.filter_and_format(info_dict, mng_address, mng_cik, mng_name, report_period)


    def parse_from_dir(self, file_paths: list[str]):
        # Go through all files and concatenate to dataframe
        filing_dfs = []
        failures = []
        for path in file_paths:
            if path.endswith('.txt'):
                print(f'parsing {path}')
                try:
                    with open(path, 'r') as file:
                        filing = self.extract_dicts(file.read())
                        tmp_filing_df = pd.DataFrame(filing)
                        cik,date,sequence = path.split('\\')[-1].split('.')[0].split('_')
                        # edgar files are formated {cik}-{yy}-{sequence}.txt
                        tmp_filing_df[SOURCE_ID_COL] = f'https://sec.gov/Archives/edgar/data/{cik}-{date[-2:]}-{sequence}.txt'
                        filing_dfs.append(tmp_filing_df)
                except Exception as e:
                    print(e)
                    failures.append(path)
        filing_df = pd.concat(filing_dfs, ignore_index=True)
        filing_df[REPORT_PERIOD_COL] = pd.to_datetime(filing_df[REPORT_PERIOD_COL]).dt.date
        filing_df[VALUE_COL] = filing_df[VALUE_COL].astype(float)
        filing_df[SHARES_COL] = filing_df[SHARES_COL].astype(np.int64)
        return filing_df, failures


    # This data contains duplicates where an asset is reported more than once for the same filing manager within the same
    # report calendar/quarter.
    # See for example https://www.sec.gov/Archives/edgar/data/1962636/000139834423009400/0001398344-23-009400.txt
    # for our intents and purposes we will sum over values and shares to aggregate the duplicates out
    def aggregate_data(self, filings_df: pd.DataFrame) -> pd.DataFrame:
        print(f'=== Aggregating Parsed Data ===')
        return filings_df.groupby([SOURCE_ID_COL, FILING_MANAGER_CIK_COL, FILING_MANAGER_ADDRESS_COL, FILING_MANAGER_NAME_COL, REPORT_PERIOD_COL,
                                  COMPANY_CUSIP6_COL, COMPANY_CUSIP_COL]) \
            .agg({COMPANY_NAME_COL: 'first', VALUE_COL: "sum", SHARES_COL: "sum"}).reset_index()


    def filter_data(self, filings_df: pd.DataFrame, top_n_periods: int) -> pd.DataFrame:
        print(f'=== Filtering Data ===')
        periods_df = filings_df[[REPORT_PERIOD_COL, VALUE_COL]] \
            .groupby(REPORT_PERIOD_COL).count().reset_index().sort_values(REPORT_PERIOD_COL)
        num_periods = min(periods_df.shape[0], top_n_periods)
        top_periods = periods_df[REPORT_PERIOD_COL][-num_periods:].tolist()
        return filings_df[filings_df[REPORT_PERIOD_COL].isin(top_periods)]
