# Tao file mp3 cho tung canh trong bai hoc.
param(
    [Parameter(Mandatory = $true)]
    [string]$Lesson
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$py = Join-Path $here ".venv\Scripts\python.exe"
if (-not (Test-Path $py)) {
    & (Join-Path $here "setup.ps1")
}
& $py (Join-Path $here "build_lesson.py") narrate --lesson $Lesson
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
