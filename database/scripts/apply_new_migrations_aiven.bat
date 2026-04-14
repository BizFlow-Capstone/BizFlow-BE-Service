@echo off
SETLOCAL EnableExtensions DisableDelayedExpansion

echo ================================================
echo    AUTO APPLY NEW MIGRATIONS (AIVEN)
echo ================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "MIGRATIONS_DIR=%SCRIPT_DIR%..\migrations"

if not exist "%MIGRATIONS_DIR%" (
    echo [ERROR] Migrations directory not found: "%MIGRATIONS_DIR%"
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

set "TEMP_FILE=%TEMP%\aiven_applied_migrations_%RANDOM%%RANDOM%.txt"
"%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% -N -e "SELECT MigrationId FROM __MigrationHistory" "%AIVEN_DB_NAME%" 2>nul > "%TEMP_FILE%"
if errorlevel 1 (
    type nul > "%TEMP_FILE%"
)

set "COUNT=0"
for /f "delims=" %%f in ('dir /b /on "%MIGRATIONS_DIR%\*.sql"') do (
    findstr /X /C:"%%~nf" "%TEMP_FILE%" >nul
    if errorlevel 1 (
        echo [NEW] Found new migration: %%f
        call set /a COUNT=%%COUNT%%+1
    )
)

del "%TEMP_FILE%" 2>nul

if "%COUNT%"=="0" (
    echo.
    echo [OK] No new migrations to apply.
    echo All migrations are up to date!
    pause
    exit /b 0
)

echo.
echo [INFO] Found %COUNT% new migration(s) to apply.
echo.
set /p CONFIRM="Do you want to apply these migrations to Aiven? (Y/N): "

if /i not "%CONFIRM%"=="Y" (
    echo.
    echo [CANCELLED] Migration cancelled by user.
    pause
    exit /b 0
)

echo.
echo ================================================
echo    APPLYING MIGRATIONS TO AIVEN...
echo ================================================
echo.

for /f "delims=" %%f in ('dir /b /on "%MIGRATIONS_DIR%\*.sql"') do (
    "%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% -N -e "SELECT COUNT(*) FROM __MigrationHistory WHERE MigrationId='%%~nf'" "%AIVEN_DB_NAME%" 2>nul | findstr /X "1" >nul
    if errorlevel 1 (
        echo [RUNNING] Applying migration: %%f...
        "%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% "%AIVEN_DB_NAME%" < "%MIGRATIONS_DIR%\%%f"
        if errorlevel 1 (
            echo [ERROR] Failed to apply migration: %%f
            echo Please check SQL and connection settings in aiven.env
            pause
            exit /b 1
        )

        echo [OK] Successfully applied: %%f
        echo.
    )
)

echo.
echo ================================================
echo    ALL AIVEN MIGRATIONS APPLIED SUCCESSFULLY!
echo ================================================
echo.
pause
