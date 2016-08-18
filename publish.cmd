%~dp0util\nuget.exe push %~dp0artifacts\*.nupkg "%NUGET_API_KEY%" -NonInteractive

@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)