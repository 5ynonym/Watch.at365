@echo off
setlocal

pushd "%~dp0"
dotnet publish "Watch\Watch.csproj" -c Release -r win-x64 --self-contained false -o "%~dp0publish" %*
set "exitCode=%ERRORLEVEL%"
popd

exit /b %exitCode%
