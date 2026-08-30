param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$project = Join-Path $root "src\NexusKathFrontier.Launcher\NexusKathFrontier.Launcher.csproj"
$publish = Join-Path $root "artifacts\launcher"
$isccCandidates = @(
    "$env:ProgramFiles(x86)\Inno Setup 7\ISCC.exe",
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe"
)

dotnet publish $project -c $Configuration -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet publish." }

$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "No se encontró Inno Setup. Instala Inno Setup 7 y vuelve a ejecutar este archivo."
}

& $iscc (Join-Path $root "installer\setup.iss")
if ($LASTEXITCODE -ne 0) { throw "Falló la creación del instalador." }

Write-Host "Instalador creado en artifacts\installer" -ForegroundColor Green
