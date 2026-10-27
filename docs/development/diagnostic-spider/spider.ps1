param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('start', 'status', 'state', 'screenshot', 'request', 'stop')]
    [string]$Action,
    [string]$Path,
    [string]$Workspace = "$env:LOCALAPPDATA\NeeView-DiagnosticSpider\$((Get-Item $PSScriptRoot).Parent.Parent.Parent.Parent.Name)",
    [string]$Executable,
    [string]$RequestJson
)

$ErrorActionPreference = 'Stop'
$profile = Join-Path $Workspace 'profile'
$artifacts = Join-Path $Workspace 'artifacts'
$descriptorPath = Join-Path $Workspace 'session.json'
function Invoke-SpiderPipe([string]$PipeName, [string]$Method, [object]$Argument) {
    $pipe = [System.IO.Pipes.NamedPipeClientStream]::new('.', $PipeName, [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::Asynchronous)
    $pipe.Connect(5000)
    $header = [System.Text.Encoding]::UTF8.GetBytes($Method)
    $json = ConvertTo-Json -InputObject $Argument -Compress -Depth 8
    $payload = [System.Text.Encoding]::UTF8.GetBytes($json)
    $writer = [System.IO.BinaryWriter]::new($pipe, [System.Text.Encoding]::UTF8, $true)
    $reader = [System.IO.BinaryReader]::new($pipe, [System.Text.Encoding]::UTF8, $true)
    $writer.Write([int]$header.Length); $writer.Write($header); $writer.Write([int]$payload.Length); $writer.Write($payload); $writer.Flush()
    $status = $reader.ReadByte()
    $length = $reader.ReadInt32()
    $bytes = $reader.ReadBytes($length)
    if ($status -ne 1) { throw "Spider request failed: $([System.Text.Encoding]::UTF8.GetString($bytes))" }
    $responseText = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($Method -eq 'DiagnosticState' -or $Method -eq 'DiagnosticRequest') {
        return $responseText | ConvertFrom-Json
    }
    return $responseText
}

function Read-Session {
    if (-not (Test-Path -LiteralPath $descriptorPath)) { throw "No spider session at $descriptorPath. Run start first." }
    $session = Get-Content -Raw -LiteralPath $descriptorPath | ConvertFrom-Json
    $proc = Get-Process -Id $session.processId -ErrorAction SilentlyContinue
    if (-not $proc -or $proc.Path -ne $session.executable) { throw 'Session process identity did not match; refusing to control a reused PID.' }
    return $session
}

switch ($Action) {
    'start' {
        New-Item -ItemType Directory -Force -Path $Workspace, $profile, $artifacts | Out-Null
        if (-not $Executable) { $Executable = Join-Path (Resolve-Path "$PSScriptRoot\..\..\..\..").Path 'NeeView\bin\x64\Debug\net10.0-windows\win-x64\NeeView.exe' }
        if (-not (Test-Path -LiteralPath $Executable)) { throw "Build NeeView first or pass -Executable: $Executable" }
        $logPath = Join-Path $profile 'Log-1.0.0.txt'
        if (Test-Path -LiteralPath $descriptorPath) {
            try { $existing = Read-Session; throw "Spider already running with PID $($existing.processId)." } catch { if ($_.Exception.Message -like 'Spider already running*') { throw } }
            Remove-Item -LiteralPath $descriptorPath
        }
        $startInfo = [System.Diagnostics.ProcessStartInfo]::new($Executable)
        $startInfo.WorkingDirectory = Split-Path $Executable
        $startInfo.UseShellExecute = $false
        $startInfo.Environment['NEEVIEW_PROFILE'] = $profile
        $startInfo.Environment['NEEVIEW_LOG_FILE'] = Join-Path $profile 'Spider.log'
        [void]$startInfo.ArgumentList.Add('--blank')
        [void]$startInfo.ArgumentList.Add('on')
        [void]$startInfo.ArgumentList.Add('--window')
        [void]$startInfo.ArgumentList.Add('normal')
        $proc = [System.Diagnostics.Process]::Start($startInfo)
        $repository = (Resolve-Path "$PSScriptRoot\..\..\..\..").Path
        $revision = (& git -C $repository rev-parse HEAD 2>$null)
        if ($LASTEXITCODE -ne 0) { $revision = 'unknown' }
        $session = [ordered]@{ protocol = 1; processId = $proc.Id; executable = (Resolve-Path $Executable).Path; startedAt = [DateTime]::UtcNow.ToString('o'); profile = $profile; workspace = $Workspace; sourceRevision = ([string]$revision).Trim() }
        $deadline = [DateTime]::UtcNow.AddSeconds(90)
        do {
            Start-Sleep -Milliseconds 500
            try {
                $spiderLog = Join-Path $profile 'Spider.log'
                if (Test-Path -LiteralPath $spiderLog) {
                    $pipeName = [regex]::Match((Get-Content -Raw $spiderLog), 'Diagnostic server started: (\S+)').Groups[1].Value
                    if ($pipeName) {
                        $state = Invoke-SpiderPipe $pipeName 'DiagnosticState' 0
                        break
                    }
                }
            } catch { if ([DateTime]::UtcNow -ge $deadline) { throw "Spider startup failed; process log/profile: $profile. $($_.Exception.Message)" } }
            if ([DateTime]::UtcNow -ge $deadline) { throw "Spider startup timed out; inspect $profile\Log-1.0.0.txt" }
        } while ($true)
        $session | Add-Member -NotePropertyName pipeName -NotePropertyValue $pipeName -Force
        $session | ConvertTo-Json | Set-Content -LiteralPath $descriptorPath
        $state | ConvertTo-Json -Depth 8
    }
    'status' {
        $session = Read-Session
        (Invoke-SpiderPipe $session.pipeName 'DiagnosticState' 0) | ConvertTo-Json -Depth 8
    }
    'state' {
        $session = Read-Session
        (Invoke-SpiderPipe $session.pipeName 'DiagnosticState' 0) | ConvertTo-Json -Depth 8
    }
    'screenshot' {
        if (-not $Path) { $Path = Join-Path $artifacts 'screenshot.png' }
        $session = Read-Session
        (Invoke-SpiderPipe $session.pipeName 'DiagnosticRequest' (@{ action = 'screenshot'; path = [IO.Path]::GetFullPath($Path) } | ConvertTo-Json -Compress)) | ConvertTo-Json -Depth 8
    }
    'request' {
        if (-not $RequestJson) { throw 'Pass -RequestJson, for example {"action":"open","path":"C:\\data\\sample.zip"}.' }
        $session = Read-Session
        (Invoke-SpiderPipe $session.pipeName 'DiagnosticRequest' $RequestJson) | ConvertTo-Json -Depth 8
    }
    'stop' {
        $session = Read-Session
        Stop-Process -Id $session.processId
        Remove-Item -LiteralPath $descriptorPath
        'Stopped diagnostic process.'
    }
}

