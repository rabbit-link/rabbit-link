%~dp0util\nuget.exe publish %~dp0artifacts\*.nupkg "%NUGET_API_KEY%"

@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)