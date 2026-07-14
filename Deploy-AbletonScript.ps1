$source = "$PSScriptRoot\src\AbletonScript\DDJ_LiveBridge"
$target = "$env:USERPROFILE\Documents\Ableton\User Library\Remote Scripts\DDJ_LiveBridge"

if (Test-Path $target) {
    Write-Host "Limpando diretório remoto antigo..." -ForegroundColor Yellow
    Remove-Item -Path $target -Recurse -Force
}

Write-Host "Copiando novos scripts para o Ableton..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path "$source\*" -Destination $target -Recurse -Force

Write-Host "Update concluído! O script DDJ_LiveBridge no Ableton está atualizado." -ForegroundColor Green
