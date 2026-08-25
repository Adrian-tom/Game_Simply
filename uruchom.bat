@echo off
setlocal
cd /d "%~dp0"

set "PY="
if exist "%USERPROFILE%\.local\bin\python3.14.exe" set "PY=%USERPROFILE%\.local\bin\python3.14.exe"
if not defined PY if exist "%USERPROFILE%\.local\bin\python3.exe" set "PY=%USERPROFILE%\.local\bin\python3.exe"

if defined PY (
    "%PY%" main.py
    goto :koniec
)

where py >nul 2>&1
if %ERRORLEVEL%==0 (
    py -3 main.py
    goto :koniec
)

where python >nul 2>&1
if %ERRORLEVEL%==0 (
    python main.py
    goto :koniec
)

echo Nie znaleziono Pythona 3.10+.
echo Zainstaluj Pythona albo popraw sciezke w uruchom.bat
pause
exit /b 1

:koniec
if errorlevel 1 pause
endlocal
