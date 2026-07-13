@echo off
setlocal

dotnet restore FlowPainter.sln
if errorlevel 1 exit /b %errorlevel%

dotnet build FlowPainter.sln -c Release --no-restore
if errorlevel 1 exit /b %errorlevel%

dotnet test FlowPainter.sln -c Release --no-build --logger "console;verbosity=normal"
exit /b %errorlevel%
