@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
setlocal
if exist dist rd /s /q dist || exit /b 1
md dist>nul || exit /b 1
set PUBLISH=dotnet publish -c Release
set RID=-r win-x64
     %PUBLISH% -o dist\fdd ^
  && %PUBLISH% -o dist\scd          %RID% ^
  && %PUBLISH% -o dist\one          %RID% /p:PublishSingleFile=true ^
  && %PUBLISH% -o dist\one+trim     %RID% /p:PublishSingleFile=true /p:PublishTrimmed=true ^
  && %PUBLISH% -o dist\one+trim+rtr %RID% /p:PublishSingleFile=true /p:PublishTrimmed=true /p:PublishReadyToRun=true /p:PublishReadyToRunShowWarnings=true
goto :EOF
