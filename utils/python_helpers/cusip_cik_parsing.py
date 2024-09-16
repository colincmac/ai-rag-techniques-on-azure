import csv
import pandas as pd
import csv
import re
import os
from collections import *
from glob import glob
from multiprocessing import Pool
from pathlib import Path

class CikCusipParser:
  def __init__(self) -> None:
    pass

  OUTPUT_CSV_FILE = "cik_cusip.csv"
  pat = re.compile(
      '(?!000000)(?!0001pt)[\( >]*[0-9A-Z]{1}[0-9]{3}[0-9A-Za-z]{2}[- ]*[0-9]{0,2}[- ]*[0-9]{0,1}[\) \n<]'
  )

  w = re.compile('\w+')

  def _parse(self, file):
      with open(file, 'r') as f:
          lines = f.readlines()

      record = 0
      cik = None
      for line in lines:
          if 'SUBJECT COMPANY' in line:
              record = 1
          if 'CENTRAL INDEX KEY' in line and record == 1:
              cik = line.split('\t\t\t')[-1].strip()
              break

      cusips = []
      record = 0
      for index,line in enumerate(lines):
          if 'CUSIP' in line:
              lines_to_search = lines[index-3:index+3]
              found = self.pat.findall(" ".join(lines_to_search))
              if found:
                  cusips.append(found[0].strip().strip('<>'))
                  break

          # if '<DOCUMENT>' in line:  # lines are after the document preamble
          #     record = 1
          # if record == 1:
          #     if 'IRS' not in line and 'I.R.S' not in line:
                  # fd = pat.findall(line)
                  # if fd:
                  #     cusip = fd[0].strip().strip('<>')
                  #     if args.debug:
                  #         print('INFO: added --- ', line, " --- extracted [",
                  #               cusip, "]")
                  #     cusips.append(cusip)
      if len(cusips) == 0:
          cusip = None
      else:
          cusip = Counter(cusips).most_common()[0][0]
          cusip = ''.join(self.w.findall(cusip))
      return [file, cik, cusip]


  def parse_cusip(self, form_name: str, input_form_dir: str, output_dir: str = "../../data/sec_data"):
      output_csv_file = f"{output_dir}/{form_name}.csv"
      if os.path.exists(input_form_dir):
          print("Parsed file already exists for form", form_name)
          return
      
      with Pool(30) as p:
          with open(output_csv_file, 'w') as w:
              wr = csv.writer(w)
              for res in p.imap(self._parse, glob(input_form_dir + '/*/*'), chunksize=100):
                  wr.writerow(res)

  def process_cusips_from_files(self, files: list[str], output_dir: str = "../../data/sec_data"):
    output_csv_file = f"{output_dir}/{self.OUTPUT_CSV_FILE}"
    df = [pd.read_csv(f, names=['f', 'cik', 'cusip']).dropna() for f in files]
    df = pd.concat(df)

    df['leng'] = df.cusip.map(len)

    df = df[(df.leng == 6) | (df.leng == 8) | (df.leng == 9)]

    df['cusip6'] = df.cusip.str[:6]

    df = df[df.cusip6 != '000000']
    df = df[df.cusip6 != '0001pt']

    df['cusip8'] = df.cusip.str[:8]

    df.cik = pd.to_numeric(df.cik)
    # Group by 'cik' and aggregate 'cusip6' into a list
    df = df[['cik', 'cusip6', 'cusip8']].drop_duplicates()
    df_aggregated = df.groupby('cik').agg({'cusip6': list, 'cusip8': list}).reset_index()

    df_aggregated.to_csv(output_csv_file, index=False)

