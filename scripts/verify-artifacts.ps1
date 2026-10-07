# MATRIX V2 Verify artifacts
if (Test-Path "MATRIX-FINAL/SHA256SUMS.txt") {
    Get-Content "MATRIX-FINAL/SHA256SUMS.txt" | ForEach-Object {
        $parts = $_ -split "\s+", 2
        if ($parts.Count -eq 2) {
            $expected = $parts[0]; $file = $parts[1].Trim(); $path = "MATRIX-FINAL/$file"
            if (Test-Path $path) {
                $actual = (Get-FileHash $path -Algorithm SHA256).Hash.ToLower()
                if ($actual -ne $expected) { Write-Host "FAIL: $file" } else { Write-Host "PASS: $file" }
            }
        }
    }
}
