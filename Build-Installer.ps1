# Build-Installer.ps1
# Script para gerar o instalador autônomo AbleToDJ v1.0.0

$ErrorActionPreference = "Stop"

# 1. Definir caminhos
$root = Get-Item $PSScriptRoot
$appProj = Join-Path $root "src\LiveBridge.App\LiveBridge.App.csproj"
$installerProj = Join-Path $root "src\AbleToDJ.Installer\AbleToDJ.Installer.csproj"
$resourcesDir = Join-Path $root "src\AbleToDJ.Installer\Resources"
$appPublishDir = Join-Path $root "src\LiveBridge.App\bin\Publish"
$scriptSourceDir = Join-Path $root "src\AbletonScript\DDJ_LiveBridge"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Iniciando compilação do AbleToDJ v1.0.0" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Garantir pasta de recursos do instalador
if (-not (Test-Path $resourcesDir)) {
    New-Item -ItemType Directory -Path $resourcesDir | Out-Null
}

# Limpar arquivos zip antigos
Remove-Item -Path (Join-Path $resourcesDir "*.zip") -ErrorAction SilentlyContinue

# 2. Compilar e publicar a aplicação ponte em C#
Write-Host "`n[1/5] Compilando aplicação ponte em modo Release (self-contained)..." -ForegroundColor Yellow
if (Test-Path $appPublishDir) {
    Remove-Item -Path $appPublishDir -Recurse -Force
}
dotnet publish $appProj -c Release -r win-x64 --self-contained true -o $appPublishDir

# 3. Compactar aplicação ponte
Write-Host "`n[2/5] Compactando binários do aplicativo ponte..." -ForegroundColor Yellow
$appZipPath = Join-Path $resourcesDir "app.zip"

# Garantir que a pasta de documentos/mapeamentos (docs) seja incluída na publicação
Copy-Item -Path (Join-Path $root "docs") -Destination (Join-Path $appPublishDir "docs") -Recurse -Force

Compress-Archive -Path "$appPublishDir\*" -DestinationPath $appZipPath -Force

# 4. Compactar scripts do Ableton
Write-Host "`n[3/5] Compactando Remote Scripts do Ableton..." -ForegroundColor Yellow
$scriptZipPath = Join-Path $resourcesDir "ableton_script.zip"
Compress-Archive -Path "$scriptSourceDir\*" -DestinationPath $scriptZipPath -Force

# Copiar ícone do projeto para recursos do instalador
Copy-Item -Path (Join-Path $root "icon.png") -Destination (Join-Path $resourcesDir "icon.png") -Force

# 5. Compilar e publicar o instalador do WPF como arquivo único autônomo
Write-Host "`n[4/5] Compilando o Instalador AbleToDJ_Installer.exe..." -ForegroundColor Yellow
$installerPublishDir = Join-Path $root "PublishInstaller"
if (Test-Path $installerPublishDir) {
    Remove-Item -Path $installerPublishDir -Recurse -Force
}
dotnet publish $installerProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -o $installerPublishDir

# 6. Mover instalador final para a raiz
$finalInstaller = Join-Path $root "AbleToDJ_Installer.exe"
Move-Item -Path (Join-Path $installerPublishDir "AbleToDJ_Installer.exe") -Destination $finalInstaller -Force

# Limpeza de pastas temporárias
Write-Host "`n[5/5] Limpando pastas de compilação temporárias..." -ForegroundColor Yellow
Remove-Item -Path $appPublishDir -Recurse -Force
Remove-Item -Path $installerPublishDir -Recurse -Force

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host "  Instalador AbleToDJ_Installer.exe criado!" -ForegroundColor Green
Write-Host "  Versão: 1.0.0 | Local: $finalInstaller" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
