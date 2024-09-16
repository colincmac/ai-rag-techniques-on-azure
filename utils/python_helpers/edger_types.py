from typing import TypedDict, List, Optional

class Mailing(TypedDict):
    street1: str
    street2: Optional[str]
    city: str
    stateOrCountry: str
    zipCode: str
    stateOrCountryDescription: str

class Business(TypedDict):
    street1: str
    street2: Optional[str]
    city: str
    stateOrCountry: str
    zipCode: str
    stateOrCountryDescription: str

class Addresses(TypedDict):
    mailing: Mailing
    business: Business

class RecentFilings(TypedDict):
    accessionNumber: List[str]
    filingDate: List[str]
    reportDate: List[str]
    acceptanceDateTime: List[str]
    form: List[str]

class Filings(TypedDict):
    recent: RecentFilings

class SubmissionsResponse(TypedDict):
    cik: str
    entityType: str
    sic: str
    sicDescription: str
    ownerOrg: str
    insiderTransactionForOwnerExists: int
    insiderTransactionForIssuerExists: int
    name: str
    tickers: List[str]
    exchanges: List[str]
    ein: str
    description: str
    website: str
    investorWebsite: str
    category: str
    fiscalYearEnd: str
    stateOfIncorporation: str
    stateOfIncorporationDescription: str
    addresses: Addresses
    phone: str
    flags: str
    formerNames: List[str]
    filings: Filings