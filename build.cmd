@echo off
setlocal

cd "%~dp0"

echo Restoring packages
dotnet restore "%~dp0src\RabbitLink"
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

echo Building package
dotnet pack "%~dp0src\RabbitLink" --configuration Release -o "%~dp0artifacts\" 
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

pause