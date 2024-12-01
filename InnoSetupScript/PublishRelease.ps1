Write-Host "[PackageSystem] Start build installer..."

if (Test-Path "../build/PublishAllOutput/PardofelisCore/runtimes") {
    Remove-Item -Path "../build/PublishAllOutput/PardofelisCore/runtimes" -Recurse -Force
}

if (Test-Path "../build/PublishAllOutput/PardofelisRunner/runtimes") {
    Remove-Item -Path "../build/PublishAllOutput/PardofelisRunner/runtimes" -Recurse -Force
}

if (Test-Path "../build/PublishAllOutput/PardofelisUI/runtimes") {
    Remove-Item -Path "../build/PublishAllOutput/PardofelisUI/runtimes" -Recurse -Force
}

if (Test-Path "../build/PublishAllOutput/PardofelisUI/ggml.dll") {
    Remove-Item -Path "../build/PublishAllOutput/PardofelisUI/ggml.dll" -Recurse -Force
}

if (Test-Path "../build/PublishAllOutput/PardofelisUI/llama.dll") {
    Remove-Item -Path "../build/PublishAllOutput/PardofelisUI/llama.dll" -Recurse -Force
}

if (Test-Path "../build/PublishAllOutput/PardofelisUI/llava_shared.dll") {
    Remove-Item -Path "../build/PublishAllOutput/PardofelisUI/llava_shared.dll" -Recurse -Force
}

if (Test-Path "../build/PublishRelease") {
    Remove-Item -Path "../build/PublishRelease" -Recurse -Force
}

Copy-Item "../build/PublishAllOutput/PardofelisUI" "../build/PublishRelease" -Recurse
Copy-Item "../runtimes" "../build/PublishRelease/runtimes" -Recurse

Copy-Item "../build/PublishAllOutput/EmailPlugin" "../build/PublishRelease/ToolCallPlugin/EmailPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/ScrennRecognitionPlugin" "../build/PublishRelease/ToolCallPlugin/ScrennRecognitionPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/SingPlugin" "../build/PublishRelease/ToolCallPlugin/SingPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/WeatherFifteenDaysPlugin" "../build/PublishRelease/ToolCallPlugin/WeatherFifteenDaysPlugin" -Recurse
Copy-Item "../build/PublishAllOutput/WeatherOneDayPlugin" "../build/PublishRelease/ToolCallPlugin/WeatherOneDayPlugin" -Recurse

Rename-Item "../build/PublishRelease/PardofelisUI.exe" "满穗AI助手.exe"

& ".\InnoSetup6\ISCC.exe" /Qp "PardofelisUI.iss"

Write-Host "[PackageSystem] End build installer."
