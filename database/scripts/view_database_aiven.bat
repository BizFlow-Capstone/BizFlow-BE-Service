@echo off
SETLOCAL EnableExtensions DisableDelayedExpansion

echo ================================================
echo    DATABASE INFORMATION (AIVEN)
echo ================================================
echo.

set "SCRIPT_DIR=%~dp0"
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

echo [1] Show all tables:
echo.
"%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% "%AIVEN_DB_NAME%" -e "SHOW TABLES;" -t

echo.
echo [2] Show migration history:
echo.
"%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% "%AIVEN_DB_NAME%" -e "SELECT * FROM __MigrationHistory ORDER BY AppliedAt DESC;" -t
if errorlevel 1 (
    echo [WARN] __MigrationHistory does not exist yet.
)

echo.
echo [3] Show estimated row count by table:
echo.
"%MYSQL_CLIENT%" --default-character-set=utf8mb4 -h "%AIVEN_DB_HOST%" -P %AIVEN_DB_PORT% -u "%AIVEN_DB_USER%" -p"%AIVEN_DB_PASSWORD%" --ssl-mode=%AIVEN_DB_SSL_MODE% %SSL_CA_ARG% "%AIVEN_DB_NAME%" -e "SELECT TABLE_NAME AS TableName, TABLE_ROWS AS EstimatedRows FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME;" -t

echo.
pause
