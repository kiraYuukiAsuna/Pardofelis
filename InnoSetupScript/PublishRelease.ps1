Write-Host "[PackageSystem] Start build installer..."

if (Test-Path "../build/PublishRelease") {
    Remove-Item -Path "../build/PublishRelease" -Recurse -Force
}

Copy-Item "../build/PublishAllOutput/PardofelisUI" "../build/PublishRelease" -Recurse

Copy-Item "../build/PublishAllOutput/EmailPlugin" "../build/PublishRelease/ToolCallPlugin/EmailPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/ScrennRecognitionPlugin" "../build/PublishRelease/ToolCallPlugin/ScrennRecognitionPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/SingPlugin" "../build/PublishRelease/ToolCallPlugin/SingPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/WeatherFifteenDaysPlugin" "../build/PublishRelease/ToolCallPlugin/WeatherFifteenDaysPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/WeatherOneDayPlugin" "../build/PublishRelease/ToolCallPlugin/WeatherOneDayPlugin" -Recurse

Rename-Item "../build/PublishRelease/PardofelisUI.exe" "满穗AI助手.exe"

& ".\InnoSetup6\ISCC.exe" /Qp "PardofelisUI.iss"

Write-Host "[PackageSystem] End build installer."
