# Build-Installer.ps1
# Script para gerar o instalador autônomo AbleToDJ v1.0.0 via Inno Setup

$ErrorActionPreference = "Stop"

# 1. Definir caminhos
$root = Get-Item $PSScriptRoot
$appProj = Join-Path $root "src\LiveBridge.App\LiveBridge.App.csproj"
$appPublishDir = Join-Path $root "src\LiveBridge.App\bin\Publish"
$issScript = Join-Path $root "installer.iss"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Iniciando compilação do AbleToDJ v1.0.0" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Função para encontrar o compilador do Inno Setup (iscc.exe)
function Find-Iscc {
    $command = Get-Command iscc -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $userPath = Join-Path $env:localappdata "Programs\Inno Setup 6\ISCC.exe"
    if (Test-Path $userPath) { return $userPath }

    $x86Path = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $x86Path) { return $x86Path }

    $x64Path = "C:\Program Files\Inno Setup 6\ISCC.exe"
    if (Test-Path $x64Path) { return $x64Path }

    return $null
}

# 2. Compilar e publicar a aplicação ponte em C#
Write-Host "`n[1/3] Compilando aplicação ponte em modo Release (framework-dependent)..." -ForegroundColor Yellow
if (Test-Path $appPublishDir) {
    Remove-Item -Path $appPublishDir -Recurse -Force
}
dotnet publish $appProj -c Release -r win-x64 -p:SelfContained=false -o $appPublishDir

# Garantir que a pasta de documentos/mapeamentos (docs) seja incluída na publicação
Copy-Item -Path (Join-Path $root "docs") -Destination (Join-Path $appPublishDir "docs") -Recurse -Force

# 3. Compilar instalador via Inno Setup
Write-Host "`n[2/3] Localizando compilador Inno Setup..." -ForegroundColor Yellow
$iscc = Find-Iscc
if (-not $iscc) {
    Write-Error "Erro: Compilador Inno Setup (ISCC.exe) não encontrado. Por favor, instale o Inno Setup."
}
Write-Host "Compilador encontrado: $iscc" -ForegroundColor Green

Write-Host "`n[3/3] Compilando o Instalador AbleToDJ_Installer.exe..." -ForegroundColor Yellow
& $iscc $issScript

# Limpeza de pastas temporárias
Write-Host "`nLimpando pastas de compilação temporárias..." -ForegroundColor Yellow
Remove-Item -Path $appPublishDir -Recurse -Force

$finalInstaller = Join-Path $root "AbleToDJ_Installer.exe"
Write-Host "`n==========================================" -ForegroundColor Green
Write-Host "  Instalador AbleToDJ_Installer.exe criado!" -ForegroundColor Green
Write-Host "  Versão: 1.0.0 | Local: $finalInstaller" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
