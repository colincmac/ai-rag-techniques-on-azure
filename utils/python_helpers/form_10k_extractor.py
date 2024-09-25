import json
from typing import Dict
import os
import re
import pandas as pd
from bs4 import BeautifulSoup
from glob import glob

class Form10kExtractor:
  def __init__(self) -> None:
    pass

  def process_directory_files(self, cik_cusip_csv: str, input_directory: str, output_directory: str):
          if not os.path.exists(output_directory):
              os.makedirs(output_directory)
          reference_df = pd.read_csv(cik_cusip_csv)

          for file in glob(f"{input_directory}/*/*"):
              file_id = os.path.splitext(os.path.split(file)[1])[0]
              cik,date,accession = file_id.split("_")
              print(f"Processing: {cik} - {date} - {file_id}")
              output_file_path = os.path.join(output_directory, f"{file_id}.json")

              if int(cik) not in reference_df['cik'].values:
                  print(f"{cik} not found in reference file")
                  continue
              reference = reference_df.loc[reference_df['cik'] == int(cik)].iloc[0]


              cusips = str(reference['cusips']).split(',')

              self.load_parse_save(file, output_file_path, cik, cusips, reference['name'], reference['exchange'], reference['ticker'], date, accession)

  def extract_10_k(self, txt: str) -> str:
      # Regex to find <DOCUMENT> tags
      doc_start_pattern = re.compile(r'<DOCUMENT>')
      doc_end_pattern = re.compile(r'</DOCUMENT>')
      # Regex to find <TYPE> tag proceeding any characters, terminating at new line
      type_pattern = re.compile(r'<TYPE>[^\n]+')
      # Create 3 lists with the span idices for each regex

      # There are many <Document> Tags in this text file, each as specific exhibit like 10-K, EX-10.17 etc
      # First filter will give us document tag start <end> and document tag end's <start>
      # We will use this to later grab content in between these tags
      doc_start_is = [x.end() for x in doc_start_pattern.finditer(txt)]
      doc_end_is = [x.start() for x in doc_end_pattern.finditer(txt)]

      # Type filter is interesting, it looks for <TYPE> with Not flag as new line, ie terminare there, with + sign
      # to look for any char afterwards until new line \n. This will give us <TYPE> followed Section Name like '10-K'
      # Once we have this, it returns String Array, below line will with find content after <TYPE> ie, '10-K'
      # as section names
      doc_types = [x[len('<TYPE>'):] for x in type_pattern.findall(txt)]
      # Create a loop to go through each section type and save only the 10-K section in the dictionary
      # there is just one 10-K section
      for doc_type, doc_start, doc_end in zip(doc_types, doc_start_is, doc_end_is):
          if doc_type == '10-K':
              return txt[doc_start:doc_end]
          else:
              return ""
          
  def beautify_text(self, txt: str) -> str:
      stg_txt = BeautifulSoup(txt, 'lxml')
      return stg_txt.get_text(strip=True)

  def extract_text(self, row: pd.Series, txt: str):
      section_txt = txt[row.start:row.sectionEnd].replace('Error! Bookmark not defined.', '')
      return self.beautify_text(section_txt)

  def extract_section_text(self, doc: str) -> Dict[str, str]:
      # Write the regex
      regex = re.compile(r'(>(Item|ITEM)(\s|&#160;|&nbsp;)(1A|1B|1\.|2|3|7A|7|8|10|11|15)\.{0,1})|((Item|ITEM)(\s*)(1A|1B|1\.|2|3|7A|7|8|10|11|15))')
      # Use finditer to math the regex
      matches = regex.finditer(doc)
      # Write a for loop to print the matches
      # Create the dataframe
      item_df = pd.DataFrame([(x.group(), x.start(), x.end()) for x in matches])
      item_df.columns = ['item', 'start', 'end']
      item_df['item'] = item_df.item.str.lower()

      item_df.replace('&#160;', ' ', regex=True, inplace=True)
      item_df.replace('&nbsp;', ' ', regex=True, inplace=True)
      item_df.replace(' ', '', regex=True, inplace=True)
      item_df.replace(r'\.', '', regex=True, inplace=True)
      item_df.replace('>', '', regex=True, inplace=True)

      all_pos_df = item_df.sort_values('start', ascending=True).drop_duplicates(subset=['item'], keep='last').set_index(
          'item')
      # Add section end using start of next section
      all_pos_df['sectionEnd'] = all_pos_df.start.iloc[1:].tolist() + [len(doc)]
      # filter to just the sections we care about
      sections = ['item1', 'item1a', 'item1b','item2','item3', 'item7', 'item7a', 'item8', 'item10', 'item11', 'item15']
      res = dict()
      # Iterate over the sections directly, accessing each from the original DataFrame
      for section in sections:
          if section in all_pos_df.index:  # Check if the section exists in the DataFrame
              row = all_pos_df.loc[section]
              res[section] = self.extract_text(row, doc).encode('utf-8', 'ignore').decode('utf-8')
      return res

  def load_parse_save(self, input_file_path: str, output_file_path: str, cik: str, cusips: list[str], name: str, primaryExchange: str, ticker: str, date: str, accession: str):
      if os.path.exists(output_file_path):
          return
      with open(input_file_path, 'r', encoding='utf-8') as file:
          raw_txt = file.read()
      print(f'Extracting 10-K from {input_file_path}')
      doc = self.extract_10_k(raw_txt)
      if doc == "":
          return

      cleaned_json_txt = self.extract_section_text(doc)
      cleaned_json_txt['cik'] = cik
      cleaned_json_txt['cusips'] = cusips
      cleaned_json_txt['name'] = name
      cleaned_json_txt['primaryExchange'] = primaryExchange
      cleaned_json_txt['ticker'] = ticker
      cleaned_json_txt['date'] = date
      cleaned_json_txt['accession '] = accession 

      with open(output_file_path, 'w', encoding='utf-8') as json_file:
          json.dump(cleaned_json_txt, json_file, indent=4, ensure_ascii=False)