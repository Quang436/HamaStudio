$msbuilds = Get-ChildItem -Path "C:\Program Files (x86)\Microsoft Visual Studio", "C:\Program Files\Microsoft Visual Studio" -Filter "MSBuild.exe" -Recurse -ErrorAction SilentlyContinue
Write-Host "Found $($msbuilds.Count) MSBuild instances:"
foreach ($m in $msbuilds) {
    Write-Host " - $($m.FullName)"
}

# Ưu tiên bản Community, Professional, hoặc Enterprise chứa đầy đủ bộ Web Workload
$targetMsbuild = $msbuilds | Where-Object { $_.FullName -like "*Community*" -or $_.FullName -like "*Professional*" -or $_.FullName -like "*Enterprise*" } | Select-Object -First 1
if (-not $targetMsbuild) {
    $targetMsbuild = $msbuilds | Select-Object -First 1
}

if ($targetMsbuild) {
    Write-Host "Using MSBuild at: $($targetMsbuild.FullName)"
    & $targetMsbuild.FullName HamaStudio.sln /t:Build /p:Configuration=Debug
} else {
    Write-Error "MSBuild.exe not found! Please open and rebuild the project in Visual Studio manually."
}
