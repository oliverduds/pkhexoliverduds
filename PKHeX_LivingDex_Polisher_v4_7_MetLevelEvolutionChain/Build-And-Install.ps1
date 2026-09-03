param(
    [string]$PKHeXPath = "",
    [string]$WorkDir = ""
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PolisherFile = Join-Path $ScriptDir "LivingDexPolisher.cs"
$CombinedDexFile = Join-Path $ScriptDir "CombinedLivingDex.cs"
$ExportAllFile = Join-Path $ScriptDir "ExportBoxToShowdown.cs"
$RareCatalogFile = Join-Path $ScriptDir "RareEventCatalog.cs"
$RareFormFile = Join-Path $ScriptDir "RareEventPickerWizardForm.cs"
$SwitchFRLGDexFile = Join-Path $ScriptDir "SwitchFRLGDex.cs"
$SwitchFRLGFormFile = Join-Path $ScriptDir "SwitchFRLGWizardForm.cs"

if (!(Test-Path $PolisherFile)) {
    throw "LivingDexPolisher.cs was not found beside this script."
}
if (!(Test-Path $CombinedDexFile)) {
    throw "CombinedLivingDex.cs was not found beside this script."
}
if (!(Test-Path $RareCatalogFile)) {
    throw "RareEventCatalog.cs was not found beside this script."
}
if (!(Test-Path $RareFormFile)) {
    throw "RareEventPickerWizardForm.cs was not found beside this script."
}
if (!(Test-Path $SwitchFRLGDexFile)) {
    throw "SwitchFRLGDex.cs was not found beside this script."
}
if (!(Test-Path $SwitchFRLGFormFile)) {
    throw "SwitchFRLGWizardForm.cs was not found beside this script."
}

if ([string]::IsNullOrWhiteSpace($PKHeXPath)) {
    $defaultCandidate = Join-Path (Split-Path -Parent $ScriptDir) "pkhex\PKHeX.exe"
    if (Test-Path $defaultCandidate) {
        $PKHeXPath = $defaultCandidate
    }
}

if ([string]::IsNullOrWhiteSpace($WorkDir)) {
    $WorkDir = Join-Path $ScriptDir "_build"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    Write-Host "The .NET SDK was not found." -ForegroundColor Yellow
    Write-Host "Install the .NET 10 SDK and run this script again."
    exit 2
}

$versionText = (& dotnet --version)
if (-not $versionText.StartsWith("10.")) {
    Write-Host "Warning: dotnet version detected: $versionText" -ForegroundColor Yellow
    Write-Host "The current santacrab2 fork targets net10.0-windows."
}

New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
$ZipPath = Join-Path $WorkDir "PKHeX-Plugins-cherrytree.zip"
$ExtractDir = Join-Path $WorkDir "src"
$RepoDir = Join-Path $ExtractDir "PKHeX-Plugins-cherrytree"

if (Test-Path $ExtractDir) {
    Remove-Item $ExtractDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ExtractDir -Force | Out-Null

Write-Host "Downloading santacrab2/PKHeX-Plugins (cherrytree)..." -ForegroundColor Green
Invoke-WebRequest `
    -Uri "https://github.com/santacrab2/PKHeX-Plugins/archive/refs/heads/cherrytree.zip" `
    -OutFile $ZipPath

Expand-Archive -Path $ZipPath -DestinationPath $ExtractDir -Force
if (!(Test-Path $RepoDir)) {
    throw "Repository folder was not found after extraction."
}

$PluginDir = Join-Path $RepoDir "AutoLegalityMod\Plugins"

# Remove older custom normalizer if someone copied it into a custom source tree.
$OldNormalizer = Join-Path $PluginDir "NormalizeLivingDexLevels.cs"
if (Test-Path $OldNormalizer) {
    Remove-Item $OldNormalizer -Force
}

Copy-Item $PolisherFile (Join-Path $PluginDir "LivingDexPolisher.cs") -Force
Copy-Item $CombinedDexFile (Join-Path $PluginDir "CombinedLivingDex.cs") -Force
Copy-Item $RareCatalogFile (Join-Path $PluginDir "RareEventCatalog.cs") -Force
Copy-Item $RareFormFile (Join-Path $PluginDir "RareEventPickerWizardForm.cs") -Force
Copy-Item $SwitchFRLGDexFile (Join-Path $PluginDir "SwitchFRLGDex.cs") -Force
Copy-Item $SwitchFRLGFormFile (Join-Path $PluginDir "SwitchFRLGWizardForm.cs") -Force
Write-Host "Including Normal + Shiny Living Dex generator, Rare Event Wizard & Switch FRLG Wizard." -ForegroundColor Green

if (Test-Path $ExportAllFile) {
    $TargetExport = Join-Path $PluginDir "ExportBoxToShowdown.cs"
    Copy-Item $TargetExport "$TargetExport.original" -Force
    Copy-Item $ExportAllFile $TargetExport -Force
    Write-Host "Including Export ALL Boxes / Box Range commands." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Compatibility fix for current cherrytree source + PKHeX.Core 26.7.7.
#
# cherrytree APILegality.cs currently calls:
#     GS64.GenerateSeed64(raw, tr, converted64)
# while PKHeX.Core 26.7.7 exposes:
#     GenerateSeed64(PKM pk, ulong seed)
#
# The project explicitly restores PKHeX.Core 26.7.7, so patch that call
# automatically before compiling.
# ---------------------------------------------------------------------------
$CoreAutoModProject = Join-Path $RepoDir "PKHeX.Core.AutoMod\PKHeX.Core.AutoMod.csproj"
$ApiLegalityFile = Join-Path $RepoDir "PKHeX.Core.AutoMod\AutoMod\APILegality.cs"

if (!(Test-Path $CoreAutoModProject)) {
    throw "PKHeX.Core.AutoMod.csproj was not found."
}
if (!(Test-Path $ApiLegalityFile)) {
    throw "APILegality.cs was not found."
}

$CoreProjectText = Get-Content $CoreAutoModProject -Raw
$ApiText = Get-Content $ApiLegalityFile -Raw

# Source in cherrytree uses 3 arguments (raw, tr, converted64) matching latest PKHeX.Core.

$Project = Join-Path $RepoDir "AutoLegalityMod\AutoModPlugins.csproj"

Write-Host "Building AutoModPlugins.dll..." -ForegroundColor Green
& dotnet build $Project -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Build failed. If another CSxxxx error appears, copy the FIRST compiler error." -ForegroundColor Red
    throw "dotnet build failed."
}

$Dll = Get-ChildItem `
    -Path (Join-Path $RepoDir "AutoLegalityMod\bin\Release") `
    -Filter "AutoModPlugins.dll" `
    -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $Dll) {
    throw "Build succeeded but AutoModPlugins.dll was not found."
}

$OutputDir = Join-Path $ScriptDir "compiled"
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$OutputDll = Join-Path $OutputDir "AutoModPlugins.dll"
Copy-Item $Dll.FullName $OutputDll -Force

Write-Host ""
Write-Host "Build complete:" -ForegroundColor Cyan
Write-Host $OutputDll

if (![string]::IsNullOrWhiteSpace($PKHeXPath)) {
    if (!(Test-Path $PKHeXPath)) {
        throw "PKHeX executable was not found at: $PKHeXPath"
    }

    $PKHeXDir = Split-Path -Parent (Resolve-Path $PKHeXPath)
    $PluginsDir = Join-Path $PKHeXDir "plugins"
    New-Item -ItemType Directory -Path $PluginsDir -Force | Out-Null

    $InstalledDll = Join-Path $PluginsDir "AutoModPlugins.dll"
    if (Test-Path $InstalledDll) {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $backup = Join-Path $PluginsDir "AutoModPlugins.dll.backup-$stamp"
        Copy-Item $InstalledDll $backup -Force
        Write-Host "Backup created:" -ForegroundColor Yellow
        Write-Host $backup
    }

    Copy-Item $OutputDll $InstalledDll -Force
    Write-Host "Installed to:" -ForegroundColor Cyan
    Write-Host $InstalledDll
}

Write-Host ""
Write-Host "Restart PKHeX manually." -ForegroundColor Yellow
