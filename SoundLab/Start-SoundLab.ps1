param([int]$Port = 8765)
$ErrorActionPreference = 'Stop'
$labRoot = $PSScriptRoot
$labUrl = "http://127.0.0.1:$Port"
$labAlive = $null
try { $labAlive = Invoke-RestMethod -Uri "$labUrl/api/health" -TimeoutSec 2 } catch {}
if ($labAlive -and $labAlive.app -ne 'shootemup-sound-lab') { throw 'This port belongs to a different app. Choose another -Port.' }
if (-not $labAlive) {
    $labCandidates = @(
        (Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'),
        (Join-Path $env:USERPROFILE 'anaconda3\python.exe')
    )
    $labPython = $labCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $labPython) { throw 'Python 3 is required. Run python SoundLab/server.py from the project instead.' }
    $labProcess = Start-Process -FilePath $labPython -ArgumentList @((Join-Path $labRoot 'server.py'),'--port',"$Port") -WorkingDirectory $labRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $labRoot 'server.log') -RedirectStandardError (Join-Path $labRoot 'server-errors.log') -PassThru
    $labProcess.Id | Set-Content -LiteralPath (Join-Path $labRoot 'server.pid')
    for ($labAttempt=0; $labAttempt -lt 30; $labAttempt++) {
        Start-Sleep -Milliseconds 100
        try { $labAlive = Invoke-RestMethod -Uri "$labUrl/api/health" -TimeoutSec 1; break } catch {}
        if ($labProcess.HasExited) { throw 'Sound Lab could not start. Read SoundLab/server-errors.log.' }
    }
    if (-not $labAlive) { throw 'Sound Lab did not become ready. Read SoundLab/server-errors.log.' }
}
Write-Output "Sound Lab: $labUrl"
Write-Output "Picks save to $(Join-Path $labRoot 'selections.json')"
