param()
$ErrorActionPreference='Stop'
$expectedVersion='0.13.0'
$taskRoot=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $taskRoot 'Builds/Windows/UntilTheLastLight.exe'
$log=Join-Path $taskRoot 'Logs/WindowsLaunch.log'
$display=Join-Path $taskRoot 'Logs/DisplayCheck.json'
$receipt=Join-Path $taskRoot 'Logs/WindowsLaunchCheck.json'
if(Test-Path -LiteralPath $receipt){$previous=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json;if($previous.build -ne $expectedVersion){Copy-Item -LiteralPath $receipt -Destination (Join-Path $taskRoot ('Logs/WindowsLaunchCheck-'+$previous.build+'.json')) -Force}}
if(Test-Path -LiteralPath $display){Copy-Item -LiteralPath $display -Destination (Join-Path $taskRoot 'Logs/DisplayCheck-previous.json') -Force;Remove-Item -LiteralPath $display}
$nativePlayer=Start-Process -FilePath $exe -ArgumentList @('-force-d3d11','-verify-display',('"'+$display+'"'),'-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
if(-not $nativePlayer.WaitForExit(55000)){$nativePlayer.Kill();throw 'The owned display-verification player exceeded 55 seconds.'}
if($nativePlayer.ExitCode -ne 0){throw ('Windows player exited '+$nativePlayer.ExitCode)}
$result=Get-Content -LiteralPath $display -Raw | ConvertFrom-Json
if(-not $result.passed -or $result.version -ne $expectedVersion -or $result.measurements.Count -ne 3){throw 'Standalone display verification failed or used an old player.'}
$errors=@(Select-String -LiteralPath $log -Pattern 'NullReferenceException|MissingReferenceException|ArgumentException|InvalidOperationException|UnityException|is not readable|Shader error|error CS|Failed to load')
if($errors.Count){throw ('Native startup errors: '+$errors.Count)}
[xml]$edit=Get-Content -LiteralPath (Join-Path $taskRoot 'Logs/EditMode.xml')
[xml]$play=Get-Content -LiteralPath (Join-Path $taskRoot 'Logs/PlayMode.xml')
if($edit.'test-run'.result -ne 'Passed' -or $play.'test-run'.result -ne 'Passed'){throw 'A test suite did not pass.'}
$buildText=Get-Content -LiteralPath (Join-Path $taskRoot 'Logs/Build.log') -Raw
$size=[regex]::Match($buildText,'WINDOWS BUILD SUCCESS: (\d+) bytes').Groups[1].Value
$gpu=[regex]::Match((Get-Content -LiteralPath $log -Raw),'Renderer:\s*([^\r\n]+)').Groups[1].Value
$smoke=Get-Content -LiteralPath (Join-Path $taskRoot 'Logs/Smoke.log') -Raw
if(-not $smoke.Contains('NORTON SMOKE PASS') -or -not $smoke.Contains('RUNTIME SMOKE PASS')){throw 'Norton/runtime integration evidence is missing.'}
if(-not $smoke.Contains('PLAYER SHADOW OPTICS SMOKE PASS')){throw 'Player shadow rendering evidence is missing.'}
if(-not $smoke.Contains('SEPARATE WEAPON HANDS PASS')){throw 'Independent left flashlight/right shotgun evidence is missing.'}
if(-not $smoke.Contains('EQUIPMENT INPUT SMOKE PASS')){throw 'Actual keyboard weapon/battery/reload evidence is missing.'}
if(-not $smoke.Contains('WOLF POPULATION PASS')){throw 'Nine clear connected wolf territories have not been verified.'}
if(-not $smoke.Contains('FOG RETREAT PASS') -or -not $smoke.Contains('HEAD LIFECYCLE PASS')){throw 'Fog retreat/head death and recurrence evidence is missing.'}
if(-not (Get-Content -LiteralPath (Join-Path $taskRoot 'Logs/IntroSmoke.log') -Raw).Contains('FOREST INTRO SMOKE PASS')){throw 'Opening/cabin transition evidence is missing.'}
$record=[ordered]@{build=$result.version;bytes=[long]$size;processId=$nativePlayer.Id;exitCode=$nativePlayer.ExitCode;graphics=('Direct3D 11 / '+$gpu);startupErrors=0;editTestsPassed=[int]$edit.'test-run'.passed;playTestsPassed=[int]$play.'test-run'.passed;unitTestsNote=('Both suites rerun and passed for the final '+$expectedVersion+' assets/code.');runtimeSmoke='passed';nortonSmoke='passed';standaloneDisplay='passed';displayMeasurements=$result.measurements;batchMode=$false;checkedAt=(Get-Date).ToString('o')}
$record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $receipt -Encoding UTF8
Write-Output ('Windows '+$result.version+' native startup/display passed; '+$size+' build bytes; '+$record.editTestsPassed+' EditMode and '+$record.playTestsPassed+' PlayMode tests.')
