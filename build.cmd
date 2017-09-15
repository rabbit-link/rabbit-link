@echo off
setlocal

cd "%~dp0"

echo Building RabbitLink package
dotnet pack "%cd%\src\RabbitLink" --configuration Release -o "%cd%\artifacts\" 
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

pause