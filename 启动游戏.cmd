@echo off
setlocal
set "GAME_EXE=%~dp0game\Builds\Windows\The Wandering City.exe"
if not exist "%GAME_EXE%" (
  echo Build not found. Open game in Unity 6000.6.0f1 and choose Wandering City / Build Windows.
  pause
  exit /b 1
)
start "" "%GAME_EXE%"
