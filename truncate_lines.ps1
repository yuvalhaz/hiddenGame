# Truncate Lines Script
# This script truncates each line in a text file to the first 50 characters

param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile,

    [Parameter(Mandatory=$false)]
    [string]$OutputFile,

    [Parameter(Mandatory=$false)]
    [int]$MaxLength = 50
)

# If no output file specified, create one with "_truncated" suffix
if (-not $OutputFile) {
    $directory = [System.IO.Path]::GetDirectoryName($InputFile)
    $filename = [System.IO.Path]::GetFileNameWithoutExtension($InputFile)
    $extension = [System.IO.Path]::GetExtension($InputFile)
    $OutputFile = Join-Path $directory "$filename`_truncated$extension"
}

# Check if input file exists
if (-not (Test-Path $InputFile)) {
    Write-Error "Input file not found: $InputFile"
    exit 1
}

# Read, truncate, and write
$lines = Get-Content $InputFile -Encoding UTF8
$truncatedLines = $lines | ForEach-Object {
    if ($_.Length -gt $MaxLength) {
        $_.Substring(0, $MaxLength)
    } else {
        $_
    }
}

$truncatedLines | Set-Content $OutputFile -Encoding UTF8

Write-Host "Done! Truncated file saved to: $OutputFile"
Write-Host "Lines processed: $($lines.Count)"
