@echo off
setlocal

cd "%~dp0"

echo Restoring packages
dotnet restore "%cd%\src\RabbitLink"
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

echo Building package
dotnet pack "%cd%\src\RabbitLink" --configuration Release -o "%cd%\artifacts\" 
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

pause