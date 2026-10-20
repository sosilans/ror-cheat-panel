# Publishes workspace changes to the GitHub repo so it matches the local cheat.
$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root

if (-not (Test-Path -LiteralPath (Join-Path $root ".git"))) {
    exit 0
}

$origin = git remote get-url origin 2>$null
if (-not $origin) {
    exit 0
}

git add -A
git diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    git push origin HEAD
    exit 0
}

$name = git config --get user.name
$email = git config --get user.email
$gitArgs = @()
if (-not $name -or -not $email) {
    $login = gh api user --jq ".login" 2>$null
    $id = gh api user --jq ".id" 2>$null
    if (-not $login) {
        exit 0
    }
    if (-not $name) { $gitArgs += @("-c", "user.name=$login") }
    if (-not $email) { $gitArgs += @("-c", "user.email=$id+$login@users.noreply.github.com") }
}

$stamp = Get-Date -Format "yyyy-MM-dd HH:mm"
& git @gitArgs commit -m "Update the cheat panel so the repo matches the workspace ($stamp)."
if ($LASTEXITCODE -ne 0) {
    exit 0
}

git push origin HEAD
exit 0
