import os
import json
from pathlib import Path
from typing import TypedDict
import urllib
import requests
import csv 
import pandas as pd

class Company(TypedDict):
  companyName: str
  ticker: str
  cik: int
  cusips: list[str]
  sector: str
  industry: str
  fullTimeEmployees: int

class Person(TypedDict):
  companyName: str
  ticker: str
  cik: int
  cusips: list[str]
  sector: str
  industry: str
  fullTimeEmployees: int

  def __init__(self, companyName: str, ticker: str, cik: int):
    self.companyName = companyName
    self.ticker = ticker
    self.cik = cik

  def to_json(self):
    return json.dumps(self, indent=4)

class FinanceInfoService:
  def __init__(self, user_name: str, email: str, company_cik_ticker_ref: pd.DataFrame):
    self.user_name = user_name
    self.email = email
    self.user_agent_header = {
        "User-Agent": f"{self.user_name} ({self.email})"
    }
    self.company_cik_ticker_ref = company_cik_ticker_ref

  def get_sec_filings(self, cik: int, output_dir: str = "../../data/sec_data"):
    pass

  def get_market_data(self, ticker: str, output_dir: str = "../../data/sec_data"):
    pass
