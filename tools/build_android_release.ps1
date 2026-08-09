$ErrorActionPreference = "Stop"

$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.5f2\Editor\Unity.exe"
$projectPath = Split-Path -Parent $PSScriptRoot
$credentialPath = "C:\Users\lww\.android\isekaislimesummoner-release-credentials.txt"
$logPath = Join-Path $projectPath "Logs\android-apk-release-build.log"

if (-not (Test-Path -LiteralPath $unityPath)) {
    throw "Unity 6000.3.5f2 was not found at $unityPath"
}
if (-not (Test-Path -LiteralPath $credentialPath)) {
    throw "Signing credential file was not found at $credentialPath"
}

$credentials = @{}
foreach ($line in Get-Content -LiteralPath $credentialPath) {
    if ($line -match "^(Keystore password|Key alias password):\s*(.+)$") {
        $credentials[$matches[1]] = $matches[2]
    }
}
if (-not $credentials["Keystore password"] -or -not $credentials["Key alias password"]) {
    throw "Signing passwords are missing from $credentialPath"
}

$env:ISEKAI_KEYSTORE_PASSWORD = $credentials["Keystore password"]
$env:ISEKAI_KEYALIAS_PASSWORD = $credentials["Key alias password"]

$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectPath,
    "-buildTarget", "Android",
    "-executeMethod", "CrossDefense.Editor.AndroidFeatureBuildHarness.BuildReleaseFromCommandLine",
    "-logFile", $logPath
)

try {
    $process = Start-Process -FilePath $unityPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Unity release build failed with exit code $($process.ExitCode). See $logPath"
    }
    Write-Host "Release APK: $projectPath\Builds\IsekaiSlimeSummoner-release.apk"
}
finally {
    Remove-Item Env:ISEKAI_KEYSTORE_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:ISEKAI_KEYALIAS_PASSWORD -ErrorAction SilentlyContinue
}
