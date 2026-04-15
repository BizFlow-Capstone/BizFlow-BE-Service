@echo off
SETLOCAL EnableExtensions DisableDelayedExpansion

echo ================================================
echo    RUN ONE MIGRATION (AIVEN)
echo ================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "MIGRATION_FILE="

if "%~1"=="" (
    call :prompt_migration_file
    if errorlevel 1 (
        pause
        exit /b 1
    )
) else (
    for %%I in ("%~1") do set "MIGRATION_FILE=%%~fI"
)

if not exist "%MIGRATION_FILE%" (
    echo [ERROR] Migration file not found: "%MIGRATION_FILE%"
    pause
    exit /b 1
)

call "%SCRIPT_DIR%load_aiven_env.bat"
if errorlevel 1 (
    echo.
    pause
    exit /b 1
)

set "SSL_CA_ARG="
if not "%AIVEN_DB_SSL_CA%"=="" (
    set "SSL_CA_ARG=--ssl-ca=""%AIVEN_DB_SSL_CA%"""
)

echo [RUNNING] Applying migration to Aiven: %MIGRATION_FILE%
echo.

"%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% "%AIVEN_DB_NAME%" < "%MIGRATION_FILE%"
if errorlevel 1 (
    echo.
    echo [ERROR] Migration failed.
    pause
    exit /b 1
)

echo.
echo [OK] Migration applied successfully on Aiven.
echo.
pause
goto :eof

:prompt_migration_file
echo [INFO] Missing migration file path argument.
echo [INPUT] Enter migration file path
echo Example: ..\migrations\004_create_user_table.sql
echo.
set /p MIGRATION_INPUT="Migration file: "
set "MIGRATION_INPUT=%MIGRATION_INPUT:"=%"

if "%MIGRATION_INPUT%"=="" (
    echo [ERROR] Migration file path is required.
    exit /b 1
)

for %%I in ("%MIGRATION_INPUT%") do set "MIGRATION_FILE=%%~fI"
if not exist "%MIGRATION_FILE%" (
    for %%I in ("%SCRIPT_DIR%..\migrations\%MIGRATION_INPUT%") do set "MIGRATION_FILE=%%~fI"
)

exit /b 0
