import re
import sys
from glob import glob
import json
import os
def extract_company_name(text):
    # Extract company name
    name_match = re.search(r"COMPANY CONFORMED NAME:\s*(.+)", text)
    company_name = name_match.group(1).strip() if name_match else None

    return company_name

def extract_cusip(text):
    # Try to find CUSIP patterns directly
    # Pattern: "CUSIP Number: 82968B103" or "CUSIP No.: G5784H106"
    pattern = re.search(r"CUSIP\s+(?:Number|No\.?)\s*[:\.]?\s*([A-Z0-9 ]{6,9})", text, re.IGNORECASE)
    if pattern:
        return pattern.group(1).replace(" ", "")
    
    # Pattern: <B>G47048 106</B> followed by (CUSIP Number)
    lines = text.splitlines()
    for i, line in enumerate(lines):
        if re.search(r"\(CUSIP Number\)", line, re.IGNORECASE):
            # Look at previous lines for possible CUSIP numbers
            for j in range(i, max(i-6, -1), -1):
                # Try to find <B>...</B>
                bold_match = re.search(r"<B>\s*([A-Z0-9 ]{6,9})\s*</B>", lines[j], re.IGNORECASE)
                if bold_match:
                    return bold_match.group(1).replace(" ", "")
                # Else, try to find plain text between tags
                text_match = re.search(r">([A-Z0-9 ]{6,9})<", lines[j], re.IGNORECASE)
                if text_match:
                    return text_match.group(1).replace(" ", "")
    
    # Pattern: In a <TD> tag containing "CUSIP No. G5784H106"
    td_matches = re.findall(r"<TD[^>]*>(.*?)</TD>", text, re.IGNORECASE | re.DOTALL)
    for td in td_matches:
        td_cusip_match = re.search(r"CUSIP\s+No\.?\s*[:\.]?\s*([A-Z0-9 ]{6,9})", td, re.IGNORECASE)
        if td_cusip_match:
            return td_cusip_match.group(1).replace(" ", "")
    
    return None

def process_directory(input_directory: str, output_dir: str):
    files = glob(f"{input_directory}/*/*")
    if not files:
        print(f'No files found in {input_directory}')
        return
    results = []
    failures = []
    for file in files:
        if file.endswith(".txt"):
            file_id = os.path.splitext(os.path.split(file)[1])[0]
            cik,date,accession = file_id.split("_")

            print("Processing", file)
            with open(file, 'r') as f:
                text = f.read()

            company_name = extract_company_name(text)
            cusip = extract_cusip(text)
            if cusip:
                results.append({
                    "CIK": cik,
                    "companyName": company_name,
                    "CUSIP": cusip,
                    "date": date,
                    "accession": accession
                })
                print("CIK:", cik)
                print("Company Name:", company_name)
                print("CUSIP:", cusip)
            else:
                failures.append(file)
    print(f'===== Processed {len(results)} files ====')
    print(f'===== Had {len(failures)} failed file parsings ====')
    print(failures)
    json.dump(results, open(f"{output_dir}/13d-data.json", 'w'), indent=4)

