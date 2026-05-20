param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'SilentlyContinue'
$ports = @(7188, 5188, 5001)
$pids = New-Object System.Collections.Generic.HashSet[int]

foreach ($p in $ports) {
    netstat -ano | Select-String ":$p" | ForEach-Object {
        $line = ($_ -replace '\s+', ' ').Trim()
        if (-not $line) { return }
        $parts = $line.Split(' ')
        $pidText = $parts[$parts.Length - 1]
        if ($pidText -match '^\d+$') {
            $id = [int]$pidText
            if ($id -gt 0) {
                [void]$pids.Add($id)
            }
        }
    }
}

if ($pids.Count -gt 0) {
    Write-Host "Killing processes using ports 7188/5188/5001: $($pids -join ', ')"
    foreach ($id in $pids) {
        taskkill /PID $id /F | Out-Null
    }
} else {
    Write-Host "No process is using ports 7188/5188/5001."
}

$ErrorActionPreference = 'Stop'
if ($NoBuild) {
    dotnet run --no-build
} else {
    dotnet run
}
