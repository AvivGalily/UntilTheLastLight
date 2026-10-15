param(
    [ValidateSet('Import','Mechanics','Generate','Validate','EditTests','PlayTests','Build','Capture','SignCapture','CreatureCapture','Smoke','Intro','IntroSmoke','IntroVoices','IntroAudio','Ambience','PlayerRefinements','PlayerSmoke','EquipmentGrips','ResearchSurfaces','ResearchEncounter','ResearchCapture','ResearchSmoke','ResearchEncounterSmoke','AquaticPursuitSmoke','Open')][string]$Task = 'Generate',
    [string]$Unity = 'C:\dev\UnityEditors\6000.3.24f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $Unity)) { throw "Unity 6000.3.24f1 not found. Pass -Unity with its full path." }
$logRoot = Join-Path $projectRoot 'Logs'
[IO.Directory]::CreateDirectory($logRoot) | Out-Null
$arguments = @('-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + (Join-Path $logRoot ($Task + '.log')) + '"'))
if ($Task -eq 'Open') { Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden; exit 0 }
$arguments += '-batchmode'
switch ($Task) {
 'Import' { $arguments += @('-quit','-nographics') }
 'Mechanics' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.GenerateMechanics') }
 'Generate' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.GeneratePrototype') }
 'Validate' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectValidator.ValidateAll') }
 'EditTests' { $arguments += @('-runTests','-testPlatform','EditMode','-testResults',(Join-Path $logRoot 'EditMode.xml')) }
 'PlayTests' { $arguments += @('-runTests','-testPlatform','PlayMode','-testResults',(Join-Path $logRoot 'PlayMode.xml')) }
 'Build' { $arguments += @('-quit','-executeMethod','Game.Editor.BuildPipeline.BuildWindows') }
 'Capture' { $arguments += @('-executeMethod','Game.Editor.VisualVerification.Capture') }
 'SignCapture' { $arguments += @('-executeMethod','Game.Editor.VisualVerification.CaptureSigns') }
 'CreatureCapture' { $arguments += @('-executeMethod','Game.Editor.CreatureVerification.Capture') }
 'Smoke' { $arguments += @('-executeMethod','Game.Editor.VisualVerification.Smoke') }
 'Intro' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.InstallForestIntro') }
 'IntroSmoke' { $arguments += @('-executeMethod','Game.Editor.IntroVerification.Run') }
 'IntroVoices' { $arguments += @('-quit','-executeMethod','Game.Editor.IntroVoiceSetup.Install') }
 'IntroAudio' { $arguments += @('-quit','-executeMethod','Game.Editor.IntroAudioSetup.InstallSupplied') }
 'Ambience' { $arguments += @('-quit','-executeMethod','Game.Editor.ForestAmbienceSetup.InstallBuildingCrickets') }
 'PlayerRefinements' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.InstallPlayerRefinements') }
 'PlayerSmoke' { $arguments += @('-executeMethod','Game.Editor.PlayerRefinementsVerification.Run') }
 'EquipmentGrips' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.InstallEquipmentGrips') }
 'ResearchSurfaces' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.InstallResearchSurfaces') }
 'ResearchEncounter' { $arguments += @('-quit','-executeMethod','Game.Editor.ProjectSetup.InstallResearchEncounter') }
 'ResearchEncounterSmoke' { $arguments += @('-executeMethod','Game.Editor.ResearchEncounterVerification.Run') }
 'AquaticPursuitSmoke' { $arguments += @('-nographics','-executeMethod','Game.Editor.AquaticPursuitVerification.Run') }
 'ResearchCapture' { $arguments += @('-executeMethod','Game.Editor.VisualVerification.CaptureResearch') }
 'ResearchSmoke' { $arguments += @('-executeMethod','Game.Editor.ResearchGameplayVerification.Run') }
}
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
# Wait for the editor, not its persistent licensing/helper process tree.
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity exited $($process.ExitCode). See $logRoot\$Task.log" }
Write-Output "Unity $Task completed. Log: $logRoot\$Task.log"
