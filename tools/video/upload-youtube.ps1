# Dang nhap Google hoac dang bai hoc len YouTube.
param(
    [string]$Lesson,
    [switch]$Login,
    [ValidateSet("public", "unlisted", "private")]
    [string]$Privacy,
    [string]$SetPublic
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$py = Join-Path $here ".venv\Scripts\python.exe"
if (-not (Test-Path $py)) {
    & (Join-Path $here "setup.ps1")
}
$script = Join-Path $here "upload_youtube.py"
if ($Login) {
    & $py $script --login
} elseif ($SetPublic) {
    & $py $script --set-public $SetPublic
} elseif ($Lesson) {
    $arg = @($script, "--lesson", $Lesson)
    if ($Privacy) { $arg += @("--privacy", $Privacy) }
    & $py @arg
} else {
    throw "Can -Lesson, -Login, hoac -SetPublic."
}
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
