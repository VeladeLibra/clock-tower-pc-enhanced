# Clock Tower PC Enhanced / RecompLauncher
# Uso: .\scripts\Empacotar-RecompLauncher.ps1 -PastaPublicada "C:\caminho\pasta-pronta" -Versao "v1.8.2"
# A pasta deve conter launcher e runtime recompilado legalmente distribuivel, sem a imagem do jogo.
param(
    [Parameter(Mandatory=$true)][string]$PastaPublicada,
    [Parameter(Mandatory=$true)][string]$Versao,
    [string]$Saida = "."
)
$ErrorActionPreference = "Stop"
$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$origem = (Resolve-Path $PastaPublicada).Path
$exeLauncher = Join-Path $origem "ClockTower_PC_Enhanced.exe"
$exeJogo = Join-Path $origem "game\ClockTowerOriginal_Recompiled.exe"
$exeJogoRaiz = Join-Path $origem "ClockTowerOriginal_Recompiled.exe"
if (!(Test-Path $exeLauncher)) { throw "O launcher nao existe: $exeLauncher" }
if (!(Test-Path $exeJogo) -and !(Test-Path $exeJogoRaiz)) {
    throw "Runtime nao encontrado. A release precisa incluir game\ClockTowerOriginal_Recompiled.exe (ou na raiz)."
}
$proibidos = Get-ChildItem -LiteralPath $origem -Recurse -File |
    Where-Object { $_.Extension.ToLowerInvariant() -in @(".iso", ".cue", ".chd", ".pbp", ".cso", ".img", ".ccd", ".sub") }
if ($proibidos.Count -gt 0) {
    throw "Imagem de jogo encontrada. Remova imagens de disco da pasta de release antes de distribuir."
}
$manifesto = Join-Path $raiz "recomplauncher.json"
if (!(Test-Path $manifesto)) { throw "recomplauncher.json ausente no repositorio." }
$dados = Get-Content -LiteralPath $manifesto -Raw | ConvertFrom-Json
if ($dados.format -ne 1 -or $dados.repo -ne "VeladeLibra/clock-tower-pc-enhanced") {
    throw "Manifesto RecompLauncher invalido."
}
$saidaAbs = [IO.Path]::GetFullPath($Saida)
New-Item -ItemType Directory -Path $saidaAbs -Force | Out-Null
$nome = "ClockTower-PC-Enhanced-$Versao-Windows-x64.zip"
$zip = Join-Path $saidaAbs $nome
$temp = Join-Path ([IO.Path]::GetTempPath()) ("ct-recomplauncher-" + [guid]::NewGuid().ToString("N"))
try {
    New-Item -ItemType Directory -Path $temp -Force | Out-Null
    Get-ChildItem -LiteralPath $origem -Force | Copy-Item -Destination $temp -Recurse -Force -ErrorAction Stop
    Copy-Item -LiteralPath $manifesto -Destination (Join-Path $temp "recomplauncher.json") -Force
    if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
    Compress-Archive -Path (Join-Path $temp "*") -DestinationPath $zip -CompressionLevel Optimal
    Add-Type -AssemblyName System.IO.Compression
    $arq = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $nomes = @($arq.Entries | ForEach-Object { $_.FullName.Replace("\","/") })
        if ("recomplauncher.json" -notin $nomes -or "ClockTower_PC_Enhanced.exe" -notin $nomes) {
            throw "ZIP nao contem launcher e manifesto na raiz."
        }
    } finally { $arq.Dispose() }
    Write-Host "Pacote gerado: $zip" -ForegroundColor Green
    Write-Host "Publique o ZIP nos assets de uma nova GitHub Release."
} finally {
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
