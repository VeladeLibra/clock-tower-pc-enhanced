param([string]$NomeRepo = 'clock-tower-pc-enhanced')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $raiz

if (!(Get-Command git -ErrorAction SilentlyContinue)) {
    if (!(Get-Command winget -ErrorAction SilentlyContinue)) { throw 'Instale o Git antes de continuar.' }
    & winget install --id Git.Git -e --source winget --accept-source-agreements --accept-package-agreements
    if ($LASTEXITCODE -ne 0) { throw 'Instalacao do Git falhou.' }
}
if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
    if (!(Get-Command winget -ErrorAction SilentlyContinue)) { throw 'Instale o GitHub CLI antes de continuar.' }
    & winget install --id GitHub.cli -e --source winget --accept-source-agreements --accept-package-agreements
    if ($LASTEXITCODE -ne 0) { throw 'Instalacao do GitHub CLI falhou.' }
}
# Atualiza PATH da sessao apos instalacao via winget.
$env:PATH = [Environment]::GetEnvironmentVariable('PATH','Machine') + ';' + [Environment]::GetEnvironmentVariable('PATH','User') + ';' + $env:PATH
if (!(Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git instalado, mas nao encontrado na sessao. Reabra o script.' }
if (!(Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI instalado, mas nao encontrado na sessao. Reabra o script.' }
Write-Host 'Verificando login no GitHub...' -ForegroundColor Cyan
& gh auth status
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Entre na sua conta GitHub para publicar.' -ForegroundColor Yellow
    & gh auth login --web --git-protocol https
    if ($LASTEXITCODE -ne 0) { throw 'Login GitHub nao concluido.' }
}

# Este pacote nao contem jogos nem instalador completo. Nunca adicionar dados locais.
$proibidos = Get-ChildItem -LiteralPath $raiz -Recurse -File | Where-Object {
    $_.Extension.ToLowerInvariant() -in @('.iso','.cue','.bin','.chd','.img','.pbp','.mdf','.mds','.rom','.psxmod','.exe','.dll','.pdb')
}
if ($proibidos) {
    $proibidos | Select-Object -ExpandProperty FullName
    throw 'Arquivos binarios ou imagem de jogo detectados. Publicacao bloqueada.'
}
if (!(Test-Path '.git')) { & git init -b main; if ($LASTEXITCODE -ne 0) { throw 'git init falhou.' } }
& git add README.md RELEASE_V181.md CHANGELOG.md .gitignore .github docs LauncherUnico PUBLICAR_GITHUB.ps1
if ($LASTEXITCODE -ne 0) { throw 'git add falhou.' }
& git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    & git -c user.name=VeladeLibra -c user.email=veladelibra@users.noreply.github.com commit -m 'Publicar codigo-fonte do Clock Tower PC Enhanced v1.8.1'
    if ($LASTEXITCODE -ne 0) { throw 'git commit falhou.' }
}
Write-Host 'Criando repositorio PUBLICO com apenas os fontes e documentos...' -ForegroundColor Cyan
& gh repo create $NomeRepo --public --source . --remote origin --push --description 'Launcher independente Clock Tower PC Enhanced - Criado por VeladeLibra'
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Se o repositorio ja existir, confira o remote e faca push manualmente.' -ForegroundColor Yellow
    throw 'Nao foi possivel criar/publicar o repositorio automaticamente.'
}
Write-Host "REPOSITORIO PUBLICADO: $NomeRepo" -ForegroundColor Green
& gh repo view --web
