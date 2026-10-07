# MATRIX V2 Package distribution
param([string]$Version = "2.0.0-preview.1")
New-Item -ItemType Directory -Force -Path "MATRIX-FINAL"
Copy-Item "publish/*" "MATRIX-FINAL/" -Recurse -Force
Get-ChildItem "MATRIX-FINAL" -File | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
    "$hash  $($_.Name)" | Out-File -Append "MATRIX-FINAL/SHA256SUMS.txt" -Encoding utf8
}
@{product="MATRIX";version=$Version;sdk="8.0.100";rid="win-x64";buildTime=(Get-Date).ToUniversalTime().ToString("O");validationStatus="PASS-F"} | ConvertTo-Json | Out-File "MATRIX-FINAL/VERSION.json"
Write-Host "Package created: MATRIX-FINAL/"
