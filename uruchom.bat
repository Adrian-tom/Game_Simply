@echo off
setlocal
cd /d "%~dp0"

rem Argumenty przechodza do gry, np.  uruchom.bat --tekst
set "ARGS=%*"

if exist "%USERPROFILE%\.local\bin\python3.14.exe" (
    call :graj "%USERPROFILE%\.local\bin\python3.14.exe"
    goto :koniec
)
if exist "%USERPROFILE%\.local\bin\python3.exe" (
    call :graj "%USERPROFILE%\.local\bin\python3.exe"
    goto :koniec
)

where py >nul 2>&1
if %ERRORLEVEL%==0 (
    call :graj py -3
    goto :koniec
)

where python >nul 2>&1
if %ERRORLEVEL%==0 (
    call :graj python
    goto :koniec
)

echo Nie znaleziono Pythona 3.10+.
echo Zainstaluj Pythona albo popraw sciezke w uruchom.bat
pause
exit /b 1

rem ---------------------------------------------------------------
rem  :graj <polecenie pythona>  — doinstaluj grafike i odpal gre
rem ---------------------------------------------------------------
:graj
%* -c "import pygame" >nul 2>&1
if errorlevel 1 (
    echo Pierwsze uruchomienie: instaluje grafike pygame-ce...
    %* -m pip install --user -r requirements.txt
    if errorlevel 1 echo Nie udalo sie zainstalowac pygame-ce - gra ruszy w trybie tekstowym.
)
%* main.py %ARGS%
exit /b %ERRORLEVEL%

:koniec
if errorlevel 1 pause
endlocal
