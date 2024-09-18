import xml.etree.ElementTree as ET
import json
import re

def normalize_name(name):
    """Normalize the company name for matching."""
    cleaned_string = re.sub(r'(?:\/The)|(?:&amp;)|&|(?:\/[A-Z]+)|[^a-zA-Z0-9\s]', '', name)
    return cleaned_string.upper().strip()

def parse_nport_xml(xml_file):
    """Parse the NPORT XML file and extract securities data."""
    namespaces = {
        'nport': 'http://www.sec.gov/edgar/nport',
        'com': 'http://www.sec.gov/edgar/common',
        'ncom': 'http://www.sec.gov/edgar/nportcommon',
        'xsi': 'http://www.w3.org/2001/XMLSchema-instance'
    }

    tree = ET.parse(xml_file)
    root = tree.getroot()

    invstOrSecs = root.findall('.//nport:invstOrSec', namespaces)

    securities = []

    for sec in invstOrSecs:
        # Extract fields
        name = sec.find('nport:name', namespaces)
        lei = sec.find('nport:lei', namespaces)
        title = sec.find('nport:title', namespaces)
        cusip = sec.find('nport:cusip', namespaces)
        identifiers = sec.find('nport:identifiers', namespaces)
        isin = None
        if identifiers is not None:
            isin_elem = identifiers.find('nport:isin', namespaces)
            if isin_elem is not None:
                isin = isin_elem.attrib.get('value')
        assetCat = sec.find('nport:assetCat', namespaces)
        issuerCat = sec.find('nport:issuerCat', namespaces)
        invCountry = sec.find('nport:invCountry', namespaces)

        # Collect the data into a dictionary
        sec_data = {
            'lei': lei.text if lei is not None else '',
            'name': name.text if name is not None else None,
            'title': title.text if title is not None else None,
            'cusip': cusip.text if cusip is not None else '',
            'isin': isin if isin is not None else '',
            'country': invCountry.text if invCountry is not None else '',
        }
        securities.append(sec_data)

    return securities

def load_json_securities(json_file):
    """Load the JSON list of securities."""
    with open(json_file, 'r') as f:
        json_securities = json.load(f)
    return json_securities

def combine_securities(nport_securities, json_securities):
    """Combine securities data from NPORT XML and JSON list."""
    # Create a mapping from normalized company name to JSON security data
    json_securities_map = {}
    for sec in json_securities:
        company_name = sec.get('companyName', '')
        normalized_name = normalize_name(company_name)
        json_securities_map[normalized_name] = sec

    combined_securities = []
    unmatched = []
    for sec in nport_securities:
        nport_name = sec.get('name', '')
        nport_title = sec.get('title', '')

        normalized_nport_name = normalize_name(nport_name)
        json_sec = json_securities_map.get(normalized_nport_name)
        if json_sec:
            sec.pop('name', None)
            sec.pop('title', None)
            # Match found, combine data
            combined_securities.append({**sec, **json_sec})
        else:
            # No match found, report only
            unmatched.append((nport_name, nport_title))
    print(f"Did not find Ticker/CIK for {len(unmatched)} entities")
    print(unmatched)
    return combined_securities

def process_nport(nport_file, cik_ticker_name_ref_file, output_dir):
    output_file = f"{output_dir}/securities_cusip_ref.json"

    nport_securities = parse_nport_xml(nport_file)
    json_securities = load_json_securities(cik_ticker_name_ref_file)

    combined_securities = combine_securities(nport_securities, json_securities)

    # Output the combined securities to a JSON file
    with open(output_file, 'w') as f:
        json.dump(combined_securities, f, indent=4)

    print(f"Combined securities data has been saved to '{output_file}'.")

