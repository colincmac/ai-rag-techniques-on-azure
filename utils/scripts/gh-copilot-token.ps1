# GitHub Device Flow - Generate User Access Token for GitHub App
# Requires PowerShell 5.1 or later

# Exit script on any errors
$ErrorActionPreference = "Stop"

# Function to check if a variable is null or empty
function IsNullOrEmpty {
    param (
        [string]$Value
    )
    return ([string]::IsNullOrEmpty($Value))
}

# GitHub Device Flow endpoints
$DeviceCodeUrl = "https://github.com/login/device/code"
$AccessTokenUrl = "https://github.com/login/oauth/access_token"

# Prompt user for Client ID
do {
    $ClientID = Read-Host "Enter your GitHub OAuth App Client ID"
    if (IsNullOrEmpty $ClientID) {
        Write-Host "Client ID cannot be empty. Please enter a valid Client ID." -ForegroundColor Red
    }
} while (IsNullOrEmpty $ClientID)


# Initiate Device Authorization Request
Write-Host "Requesting device code from GitHub..." -ForegroundColor Green

$DeviceResponse = Invoke-RestMethod -Method Post -Uri $DeviceCodeUrl -Body @{
    client_id = $ClientID
} -Headers @{ Accept = "application/json" }

# Check for errors in the response
if ($DeviceResponse.error) {
    Write-Host "Error obtaining device code: $($DeviceResponse.error_description)" -ForegroundColor Red
    exit 1
}

# Extract required fields from the response
$DeviceCode     = $DeviceResponse.device_code
$UserCode       = $DeviceResponse.user_code
$VerificationUri = $DeviceResponse.verification_uri
$ExpiresIn      = [int]$DeviceResponse.expires_in
$Interval       = [int]$DeviceResponse.interval

# Display instructions to the user
Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Yellow
Write-Host "To authorize, please perform the following steps:" -ForegroundColor Yellow
Write-Host "1. Open your browser and navigate to: $VerificationUri" -ForegroundColor Yellow
Write-Host "2. Enter the user code: $UserCode" -ForegroundColor Yellow
Write-Host "=====================================================================" -ForegroundColor Yellow
Write-Host ""

# Record the start time
$StartTime = Get-Date

# Start polling for the access token
Write-Host "Waiting for authorization..." -ForegroundColor Green

$Token = $null
$Polling = $true

while ($Polling) {
    Start-Sleep -Seconds $Interval

    try {
        $AccessResponse = Invoke-RestMethod -Method Post -Uri $AccessTokenUrl -Body @{
            client_id   = $ClientID
            device_code = $DeviceCode
            grant_type  = "urn:ietf:params:oauth:grant-type:device_code"
        } -Headers @{ Accept = "application/json" }
    }
    catch {
        Write-Host "Error during token polling: $_" -ForegroundColor Red
        exit 1
    }

    if ($AccessResponse.access_token) {
        $Token = $AccessResponse.access_token
        Write-Host ""
        Write-Host "=====================================================================" -ForegroundColor Green
        Write-Host "Authorization successful!" -ForegroundColor Green
        Write-Host "Your access token is saved to the $TOKEN variable" -ForegroundColor Green
        # Write-Host "Your access token is:" -ForegroundColor Green
        # Write-Host "$Token" -ForegroundColor Green
        Write-Host ""
        Write-Host "Please store this token securely. You can now use it to authenticate API requests." -ForegroundColor Green
        Write-Host "=====================================================================" -ForegroundColor Green
        $Polling = $false
        $env:Token= $Token
        break
    }

    # Handle errors
    if ($AccessResponse.error) {
        switch ($AccessResponse.error) {
            "authorization_pending" {
                # Continue polling
                Write-Host "Authorization pending. Continuing to poll..." -ForegroundColor Yellow
            }
            "slow_down" {
                # Increase interval by 5 seconds
                $Interval += 5
                Write-Host "Received 'slow_down' response. Increasing polling interval to $Interval seconds." -ForegroundColor Yellow
            }
            "access_denied" {
                Write-Host "Error: The user denied the request." -ForegroundColor Red
                exit 1
            }
            "expired_token" {
                Write-Host "Error: The device code has expired. Please restart the process." -ForegroundColor Red
                exit 1
            }
            default {
                Write-Host "An unexpected error occurred: $($AccessResponse.error)" -ForegroundColor Red
                exit 1
            }
        }
    }

    # Calculate elapsed time
    $CurrentTime = Get-Date
    $Elapsed = ($CurrentTime - $StartTime).TotalSeconds

    # Check if the device code has expired
    if ($Elapsed -ge $ExpiresIn) {
        Write-Host "Error: The device code has expired. Please restart the process." -ForegroundColor Red
        exit 1
    }
}

# Optionally, you can output the token to a file or environment variable securely
# For example, to set it as an environment variable (session-only):
# $env:GITHUB_ACCESS_TOKEN = $Token
