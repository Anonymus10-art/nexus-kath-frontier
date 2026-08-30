param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [string]$PackDirectory = (Join-Path $PSScriptRoot "..\pack"),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\release-assets")
)

$ErrorActionPreference = "Stop"
$packRoot = [System.IO.Path]::GetFullPath($PackDirectory)
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)

if (-not (Test-Path -LiteralPath $packRoot -PathType Container)) {
    throw "No existe la carpeta del modpack: $packRoot"
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
Get-ChildItem -LiteralPath $outputRoot -File | Remove-Item -Force

$files = foreach ($file in Get-ChildItem -LiteralPath $packRoot -File -Recurse | Sort-Object FullName) {
    $relative = [System.IO.Path]::GetRelativePath($packRoot, $file.FullName).Replace("\", "/")
    $assetName = $relative.Replace("/", "__")
    $assetPath = Join-Path $outputRoot $assetName
    Copy-Item -LiteralPath $file.FullName -Destination $assetPath -Force

    $encodedAsset = [uri]::EscapeDataString($assetName)
    [ordered]@{
        path   = $relative
        url    = "https://github.com/$Repository/releases/download/pack-$Version/$encodedAsset"
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        size   = $file.Length
    }
}

$manifest = [ordered]@{
    packVersion     = $Version
    minecraftVersion = "1.21.1"
    neoForgeVersion  = "21.1.248"
    files            = @($files)
}

$manifestPath = Join-Path $outputRoot "manifest.json"
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

Write-Host "Manifiesto creado: $manifestPath" -ForegroundColor Green
Write-Host "Archivos preparados: $($files.Count)" -ForegroundColor Cyan
Write-Host "Sube todo el contenido de release-assets a la release pack-$Version." -ForegroundColor Yellow
