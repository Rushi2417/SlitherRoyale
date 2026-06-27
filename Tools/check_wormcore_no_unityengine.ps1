param (
    [string]$CorePath = "Assets\Core"
)

$foundIssues = $false
$files = Get-ChildItem -Path $CorePath -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -match '\busing\s+UnityEngine\b') {
        Write-Host "FAIL: $($file.Name) contains 'using UnityEngine'" -ForegroundColor Red
        $foundIssues = $true
    }
    if ($content -match '\bUnityEngine\.') {
        Write-Host "FAIL: $($file.Name) references UnityEngine." -ForegroundColor Red
        $foundIssues = $true
    }
}

if ($foundIssues) {
    Write-Host "WormCore validation FAILED. Zero UnityEngine references allowed." -ForegroundColor Red
    exit 1
} else {
    Write-Host "WormCore validation PASSED. No UnityEngine references found." -ForegroundColor Green
    exit 0
}
