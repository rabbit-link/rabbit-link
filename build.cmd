md %~dp0artifacts

"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" /m /p:Configuration=Release "src\RabbitLink.sln"

@if %errorlevel% equ 0 (%~dp0util\nuget.exe pack %~dp0src\RabbitLink\RabbitLink.csproj -OutputDirectory %~dp0artifacts -Properties Configuration=Release)

pause