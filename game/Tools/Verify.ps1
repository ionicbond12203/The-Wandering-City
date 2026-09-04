param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$reports = Join-Path (Split-Path $project) 'artifacts'
New-Item -ItemType Directory -Force $reports | Out-Null
foreach ($mode in @('EditMode', 'PlayMode')) {
    $result = Join-Path $reports ($mode.ToLower() + '-results.xml')
    $log = Join-Path $reports ('unity-' + $mode.ToLower() + '.log')
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'), '-runTests', '-testPlatform', $mode, '-testResults', ('"' + $result + '"'), '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "$mode failed. See $log" }
    [xml]$report = Get-Content -LiteralPath $result
    if ($report.'test-run'.result -ne 'Passed') { throw "$mode tests failed. See $result" }
    Write-Output "$mode passed: $($report.'test-run'.passed)"
}
if ($Build) {
    $log = Join-Path $reports 'unity-build.log'
    $arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'WanderingCity.Editor.ProjectBuilder.BuildWindows', '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Build failed. See $log" }
    Write-Output "Windows build: $project\Builds\Windows\The Wandering City.exe"
}
