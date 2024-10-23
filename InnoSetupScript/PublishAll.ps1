# publish-all.ps1

# 设置变量
$rootPath = ".."  # 设置为你的项目根目录
$configuration = "Release"
$framework = "net8.0"
$outputBaseFolder = "../build/PublishAllOutput"  # 设置发布输出的基础目录

# 创建输出基础目录（如果不存在）
if (-not (Test-Path $outputBaseFolder)) {
    New-Item -ItemType Directory -Path $outputBaseFolder | Out-Null
}

# 获取所有包含 .csproj 文件的文件夹
$projectFolders = Get-ChildItem -Path $rootPath -Recurse -Filter *.csproj | 
Select-Object -ExpandProperty DirectoryName -Unique

foreach ($folder in $projectFolders) {
    # 获取项目名称（使用文件夹名作为项目名）
    $projectName = Split-Path $folder -Leaf

    $selfcontained = $false
    
    if ($projectName -eq "PardofelisUI" -or $projectName -eq "PardofelisRunner") {
        $selfcontained = $true
    }
    
    # 设置此项目的输出路径
    $outputPath = Join-Path $outputBaseFolder $projectName

    Write-Host "正在发布项目: $projectName" -ForegroundColor Cyan

    # 清理旧的发布文件
    if (Test-Path $outputPath) {
        Remove-Item -Path $outputPath -Recurse -Force
    }

    # 发布项目
    $projectFile = Get-ChildItem -Path $folder -Filter *.csproj | Select-Object -First 1
    
    if ($selfcontained -eq $true) {
        dotnet publish $projectFile.FullName `
            --configuration $configuration `
            --framework $framework `
            --output $outputPath `
            --runtime win-x64 `
            --self-contained true
    }
    else {
        dotnet publish $projectFile.FullName `
            --configuration $configuration `
            --framework $framework `
            --output $outputPath `
            --runtime win-x64
    }

    # 压缩发布文件（可选）
    # $zipPath = Join-Path $outputBaseFolder "$projectName-$configuration.zip"
    # Compress-Archive -Path "$outputPath\*" -DestinationPath $zipPath -Force

    Write-Host "项目 $projectName 发布完成！输出路径: $outputPath" -ForegroundColor Green
    Write-Host "ZIP 文件: $zipPath" -ForegroundColor Green
    Write-Host ""
}

Write-Host "所有项目发布完成！" -ForegroundColor Yellow
