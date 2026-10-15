$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$forestVoice = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $forestVoice.SelectVoice('Microsoft David Desktop')
    $forestVoice.Rate = 2
    $forestVoice.SetOutputToWaveFile((Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'Assets\_Game\Audio\victim_help.wav'))
    $forestVoice.Speak('Help! Please, help me!')
} finally { $forestVoice.Dispose() }
