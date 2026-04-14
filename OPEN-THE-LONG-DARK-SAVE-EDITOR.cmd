@echo off
powershell -NoProfile -Command "Get-Process CodexTldSaveEditor*,TheLongDarkSaveEditor -ErrorAction SilentlyContinue | Stop-Process -Force"
cd /d "%~dp0release\TheLongDarkSaveEditor"
start "" "TheLongDarkSaveEditor.exe"
