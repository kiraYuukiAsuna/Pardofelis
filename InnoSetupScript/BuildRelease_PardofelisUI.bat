echo [PackageSystem] Start build installer...
if exist "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\ggml.dll" del "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\ggml.dll"
if exist "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\llama.dll" del "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\llama.dll"
if exist "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\llava_shared.dll" del "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\llava_shared.dll"

if not exist "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\����AI����.exe" (
    ren "..\PardofelisUI\bin\Release\net8.0\publish\win-x64\PardofelisUI.exe" "����AI����.exe"
)

InnoSetup6\ISCC.exe /Qp PardofelisUI.iss
echo [PackageSystem] End build installer.
