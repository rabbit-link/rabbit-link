md %~dp0artifacts

%~dp0util\nuget.exe restore %~dp0src\RabbitLink.sln

@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" /m /nologo "/consoleloggerparameters:PerformanceSummary;Summary" /verbosity:minimal /p:Configuration=Release "src\RabbitLink.sln"

@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

%~dp0util\nuget.exe pack %~dp0src\RabbitLink\RabbitLink.csproj -OutputDirectory %~dp0artifacts -Properties Configuration=Release

pause