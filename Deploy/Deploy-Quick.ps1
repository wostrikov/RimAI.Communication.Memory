# ���ٲ���ű����޲�����
# ���벢���� Mod �� RimWorld

Write-Host "���ڲ��� RimTalk-ExpandMemory..." -ForegroundColor Cyan

# ����
dotnet build Source\RimAI.Communication.Memory.csproj --configuration Release --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "? ����ɹ�" -ForegroundColor Green
    
    # ����
    $TargetPath = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandMemory"
    
    # ����Ŀ¼
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
    
    # �����ļ�
    Copy-Item -Path "About" -Destination "$TargetPath\About" -Recurse -Force
    Copy-Item -Path "1.6" -Destination "$TargetPath\1.6" -Recurse -Force
    Copy-Item -Path "Languages" -Destination "$TargetPath\Languages" -Recurse -Force
    
    if (Test-Path "Textures") {
        Copy-Item -Path "Textures" -Destination "$TargetPath\Textures" -Recurse -Force
    }
    
    Write-Host "? �������: $TargetPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "���� RimWorld Mod ������������ Mod" -ForegroundColor Yellow
} else {
    Write-Host "? ����ʧ��" -ForegroundColor Red
}

