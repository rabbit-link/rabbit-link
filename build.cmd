@echo off
setlocal

cd "%~dp0"

echo Building RabbitLink package
dotnet pack "%cd%\src\RabbitLink" --configuration Release -o "%cd%\artifacts\" 
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

echo Building RabbitLink.Serialization package
dotnet pack "%cd%\src\RabbitLink.Serialization" --configuration Release -o "%cd%\artifacts\" 
@if %errorlevel% neq 0 ( pause & exit /b %errorlevel%)

pause