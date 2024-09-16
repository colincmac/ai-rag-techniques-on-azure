import os
import json
from pathlib import Path
from typing import TypedDict
import urllib
import requests
import csv 

class SecCompanyTicker(TypedDict):
  cik: int
  ticker: str
  companyName: str
  def __init__(self, cik: int, ticker: str, companyName: str):
    self.cik = cik
    self.ticker = ticker
    self.companyName = companyName

  def to_json(self):
    return json.dumps(self, indent=4)

class EdgarService:
  SEC_TICKER_URL = "https://www.sec.gov/files/company_tickers.json"
  SEC_TICKER_FILENAME = "sec_company_tickers.json"
  MASTER_INDEX_FILENAME = "master.idx"
  MASTER_INDEX_CSV_FILENAME = "master.csv"

  def __init__(self, user_name: str, email: str, master_idx_filename: str = MASTER_INDEX_FILENAME, sec_ticker_filename = SEC_TICKER_FILENAME):
    self.user_name = user_name
    self.email = email
    self.user_agent_header = {
        "User-Agent": f"{self.user_name} ({self.email})"
    }
    self.master_idx_filename = master_idx_filename
    self.sec_ticker_filename = sec_ticker_filename

  def get_company_tickers(self, output_file_dir: str = "../../data/sec_data") -> list[SecCompanyTicker] | None: 
    output_file = f"{output_file_dir}/{self.sec_ticker_filename}"
    if os.path.exists(output_file):
        with open(output_file, "r") as jsonfile:
            return json.load(jsonfile, object_hook=lambda d: SecCompanyTicker(**d))
        return result
      
    ticker_response = requests.get(self.SEC_TICKER_URL, headers=self.user_agent_header)
    data = json.loads(ticker_response.content)
    result = None
    with open(output_file, "w") as jsonfile:
        result = [SecCompanyTicker(cik=item["cik_str"], ticker=item["ticker"], companyName=item["title"])
            for item in data.values()]
        json.dump(result, jsonfile, indent=4)
    return result
      
  def get_master_index_file(self, start_year: int, end_year: int, output_dir: str = "../../data/sec_data"):
      output_file = f"{output_dir}/{self.master_idx_filename}"
      with open(output_file, "wb") as f:
          for year in range(start_year, end_year):
              for q in range(1, 5):
                  print(year, q)
                  content = requests.get(
                      f"https://www.sec.gov/Archives/edgar/full-index/{year}/QTR{q}/master.idx",
                      headers=self.user_agent_header,
                  ).content
                  f.write(content)

  def parse_master_index_file(self, cik_filter: list[str], output_dir: str = "../../data/sec_data"):
    output_index_csv_file = f"{output_dir}/{self.MASTER_INDEX_CSV_FILENAME}"
    master_index_file = f"{output_dir}/{self.master_idx_filename}"

    if not os.path.exists(master_index_file):
        print("Master index file not found. First run `get_master_index_file`")
        return
    
    with open(output_index_csv_file, "w", errors="ignore") as csvfile:
        wr = csv.writer(csvfile)
        wr.writerow(["cik", "comnam", "form", "date", "url"])
        with open(master_index_file, "r", encoding="latin1") as f:
            for r in f:
                if r.startswith("CIK") or r.startswith("--"):
                    continue
                split = r.split("|")
                cik_value = split[0]
                if cik_value in cik_filter:
                    if ".txt" in r:
                        wr.writerow(r.strip().split("|"))

  def download_filings_for_form(self, form: str, output_dir: str = "../../data/sec_data"):
      master_index_csv_file = f"{output_dir}/{self.MASTER_INDEX_CSV_FILENAME}"
      if not os.path.exists(master_index_csv_file):
          print("Master index file not found. First run `parse_master_index_file`")
          return
      
      print(f"Downloading Form {form} filings for {cik} to folder {output_dir}/{form}")

      to_dl = []
      with open(master_index_csv_file, "r") as f:
          reader = csv.DictReader(f)
          for row in reader:
              if form in row["form"]:
                  to_dl.append(row)

      len_ = len(to_dl)
      print(len_)
      print("start to download")

      for n, row in enumerate(to_dl):
          print(f"{n} out of {len_}")
          cik = row["cik"].strip()
          date = row["date"].strip()
          year = row["date"].split("-")[0].strip()
          month = row["date"].split("-")[1].strip()
          url = row["url"].strip()
          accession = url.split(".")[0].split("-")[-1]
          Path(f"{output_dir}/{form}/{year}_{month}").mkdir(parents=True, exist_ok=True)
          file_path = f"{output_dir}/{form}/{year}_{month}/{cik}_{date}_{accession}.txt"
          if os.path.exists(file_path):
              continue
          try:
              txt = requests.get(
                  f"https://www.sec.gov/Archives/{url}", headers=self.user_agent_header, timeout=60
              ).text
              with open(file_path, "w", errors="ignore") as f:
                  f.write(txt)
          except:
              print(f"{cik}, {date} failed to download")

  def get_company_facts(self, cik: str) -> dict:
    url = f"https://data.sec.gov/api/xbrl/companyfacts/CIK{cik.rjust(10, '0')}.json"
    request = urllib.request.Request(url, None, self.user_agent_header)
    try:
      with urllib.urlopen(request) as response:
        data = json.loads(response.read())
        return data
      response = urlopen(url)
      data = json.loads(response.read())
      return data
    except urllib.error.HTTPError as e:
      return {"error": f"HTTPError: {e}"}
    except urllib.error.URLError as e:
      return {"error": f"URLError: {e}"}
    except Exception as e:
      return {"error": f"Exception: {e}"}
  
  def get_company_submissions(self, cik: str):
      url = f"https://data.sec.gov/submissions/CIK{cik.rjust(10, '0')}.json"
      