param(
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $Blender)) { throw "Blender not found. Pass -Blender with its full path." }
$logRoot = Join-Path $projectRoot 'Logs'
[IO.Directory]::CreateDirectory($logRoot) | Out-Null
& python (Join-Path $PSScriptRoot 'prepare_cabin_sign.py')
if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare the CC0 cabin sign.' }
& python (Join-Path $PSScriptRoot 'prepare_crawler_mouth.py')
if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare the original crawler mouth texture.' }
foreach ($task in @('generate_assets', 'export_assets')) {
    $scriptPath = Join-Path $PSScriptRoot ("Blender\$task.py")
    $arguments = @('--background', '--disable-autoexec', '--python-exit-code', '1', '--python', ('"' + $scriptPath + '"'), '--', '--project', ('"' + $projectRoot + '"'))
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $Blender
    $process.StartInfo.Arguments = $arguments -join ' '
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $null = $process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    [IO.File]::WriteAllText((Join-Path $logRoot "$task.log"), $stdout.Result)
    [IO.File]::WriteAllText((Join-Path $logRoot "$task-stderr.log"), $stderr.Result)
    if ($process.ExitCode -ne 0) { throw "Blender $task failed: exit $($process.ExitCode). See $logRoot." }
    Write-Output "Blender $task completed."
}
& python (Join-Path $PSScriptRoot 'download_forest_assets.py')
if ($LASTEXITCODE -ne 0) { throw 'Failed to restore the photographed PBR surfaces.' }
