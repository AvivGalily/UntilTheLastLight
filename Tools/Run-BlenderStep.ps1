param([Parameter(Mandatory=$true)][string]$Script,[string[]]$ScriptArguments=@(),[string]$LogName='BlenderStep')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$process=[System.Diagnostics.Process]::new()
$process.StartInfo.FileName='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$process.StartInfo.Arguments='--background --disable-autoexec --python-exit-code 1 --python "'+(Join-Path $root $Script)+'"'
if($ScriptArguments.Count){$process.StartInfo.Arguments+=' -- '+($ScriptArguments -join ' ')}
$process.StartInfo.UseShellExecute=$false;$process.StartInfo.CreateNoWindow=$true
$process.StartInfo.RedirectStandardOutput=$true;$process.StartInfo.RedirectStandardError=$true
$null=$process.Start();$stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync();$process.WaitForExit()
[IO.File]::WriteAllText((Join-Path $root ('Logs/'+$LogName+'.log')),$stdout.Result)
[IO.File]::WriteAllText((Join-Path $root ('Logs/'+$LogName+'-stderr.log')),$stderr.Result)
if($process.ExitCode -ne 0){throw ('Blender failed: '+$LogName+' exit '+$process.ExitCode)}
Write-Output ('Blender completed: '+$LogName)
