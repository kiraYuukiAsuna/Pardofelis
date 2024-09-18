echo [PackageSystem] Start build installer...
if exist "..\PardofelisRunner\bin\Release\net8.0\publish\win-x64\ggml.dll" del "..\PardofelisRunner\bin\Release\net8.0\publish\win-x64\ggml.dll"
if exist "..\PardofelisRunner\bin\Release\net8.0\publish\win-x64\llama.dll" del "..\PardofelisRunner\bin\Release\net8.0\publish\win-x64\llama.dll"
if exist "..\PardofelisRunner\bin\Release\net8.0\publish\win-x64\llava_shared.dll" del "..\PardofelisRunner\bin\Release\net8.0\publish\win-x64\llava_shared.dll"
InnoSetup6\ISCC.exe /Qp PardofelisRunner.iss
echo [PackageSystem] End build installer.
