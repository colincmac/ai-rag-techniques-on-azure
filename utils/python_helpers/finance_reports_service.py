import os
import json
from pathlib import Path
from typing import TypedDict
import urllib
import requests
import csv 
import pandas as pd
import yfinance as yf
from requests import Session
from requests_cache import CacheMixin, SQLiteCache
from requests_ratelimiter import LimiterMixin, MemoryQueueBucket
from pyrate_limiter import Duration, RequestRate, Limiter

import nest_asyncio
import os

nest_asyncio.apply()

class CachedLimiterSession(CacheMixin, LimiterMixin, Session):
    pass



class FinanceReportsService:
  def __init__(self, user_name: str, email: str, company_cik_ticker_ref: pd.DataFrame, output_dir: str = "../../data/sec_data"):
    self.user_name = user_name
    self.email = email
    self.user_agent_header = {
        "User-Agent": f"{self.user_name} ({self.email})"
    }
    self.company_cik_ticker_ref = company_cik_ticker_ref
    self.output_dir = output_dir
    self.session = CachedLimiterSession(
        limiter=Limiter(RequestRate(2, Duration.SECOND*5)),  # max 2 requests per 5 seconds
        bucket_class=MemoryQueueBucket,
        backend=SQLiteCache(f"{self.output_dir}/yfinance.cache"),
    )

  def get_company_by_cik(self, cik: int):
    return self.company_cik_ticker_ref[self.company_cik_ticker_ref["cik"] == cik]

  def get_company_by_ticker(self, ticker: str):
    return self.company_cik_ticker_ref[self.company_cik_ticker_ref["ticker"] == ticker]

  def get_sec_filings(self, cik: int, output_dir: str = "../../data/sec_data"):
    pass

  def get_current_market_data(self, cik: int):
    output_file = f"{self.output_dir}/{cik}.json"
    print(cik)
    ticker = self.get_company_by_cik(cik)["ticker"].values[0]
    print(ticker)
    response = yf.Ticker(ticker, session=self.session)
    info = response.financials
    return info

  
  def get_fomc_report(self, output_dir: str = "../../data/fomc_data"):
    pass