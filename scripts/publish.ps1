# MATRIX V2 Publish self-contained
param([string]$Rid = "win-x64")
dotnet publish src/MATRIX.App/MATRIX.App.csproj -c Release -r $Rid --self-contained -o publish
Write-Host "Published to publish/ "
