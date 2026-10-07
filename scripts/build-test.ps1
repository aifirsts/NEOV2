# MATRIX V2 Build and Test
param([string]$Config = "Release")
Write-Host "=== MATRIX V2 Build and Test ==="
dotnet restore MATRIX.sln
dotnet build MATRIX.sln -c $Config --no-restore
dotnet test MATRIX.sln -c $Config --no-build --logger "console;verbosity=detailed"
python tools/test_matrix_v2.py
Write-Host "=== Complete ==="
