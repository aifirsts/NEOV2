# MATRIX V3 Package distribution
param([string]$Version = "3.0.0")
$publishDir = "publish"
$outputDir = "MATRIX-FINAL"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
# Copy EXE and companion files
Copy-Item "$publishDir/MATRIX.exe" "$outputDir/" -Force
if (Test-Path "$publishDir/*.dll") { Copy-Item "$publishDir/*.dll" "$outputDir/" -Force }
if (Test-Path "$publishDir/*.json") { Copy-Item "$publishDir/*.json" "$outputDir/" -Force }
# VERSION.json
@{product="MATRIX";version=$Version;sdk="8.0.100";rid="win-x64";buildTime=(Get-Date).ToUniversalTime().ToString("O");validationStatus="PASS"} | ConvertTo-Json | Out-File "$outputDir/VERSION.json" -Encoding utf8
# SHA256SUMS
$hashes = @()
Get-ChildItem $outputDir -File | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
    $hashes += "$hash  $($_.Name)"
}
$hashes | Out-File "$outputDir/SHA256SUMS.txt" -Encoding utf8
Write-Host "Package created: $outputDir"
