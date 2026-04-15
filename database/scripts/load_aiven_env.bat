@echo off

set "SCRIPT_DIR=%~dp0"
if "%~1"=="" (
    set "ENV_FILE=%SCRIPT_DIR%aiven.env"
) else (
    set "ENV_FILE=%~1"
)

if not exist "%ENV_FILE%" (
    echo [ERROR] Missing config file: "%ENV_FILE%"
    echo Please copy "aiven.env.example" to "aiven.env" and update credentials.
    exit /b 1
)

for /f "usebackq eol=# tokens=1* delims==" %%A in ("%ENV_FILE%") do (
    if not "%%A"=="" (
        echo(%%A| findstr /B /I "AIVEN_" >nul
        if not errorlevel 1 (
            set "%%A=%%B"
        )
    )
)

if "%AIVEN_DB_HOST%"=="" (
    echo [ERROR] Missing AIVEN_DB_HOST in "%ENV_FILE%"
    exit /b 1
)

if "%AIVEN_DB_NAME%"=="" (
    echo [ERROR] Missing AIVEN_DB_NAME in "%ENV_FILE%"
    exit /b 1
)

if "%AIVEN_DB_USER%"=="" (
    echo [ERROR] Missing AIVEN_DB_USER in "%ENV_FILE%"
    exit /b 1
)

if "%AIVEN_DB_PASSWORD%"=="" (
    echo [ERROR] Missing AIVEN_DB_PASSWORD in "%ENV_FILE%"
    exit /b 1
)

if "%AIVEN_DB_PORT%"=="" (
    set "AIVEN_DB_PORT=3306"
)

if "%AIVEN_DB_SSL_MODE%"=="" (
    set "AIVEN_DB_SSL_MODE=REQUIRED"
)

if /I "%AIVEN_DB_SSL_CA:~0,11%"=="-----BEGIN " (
    echo [ERROR] AIVEN_DB_SSL_CA must be a file path, not certificate content.
    echo Save cert to a .pem file and set AIVEN_DB_SSL_CA=full_path_to_pem_file
    exit /b 1
)

set "MYSQL_CLIENT="

if not "%AIVEN_MYSQL_CLIENT_PATH%"=="" (
    if exist "%AIVEN_MYSQL_CLIENT_PATH%" (
        set "MYSQL_CLIENT=%AIVEN_MYSQL_CLIENT_PATH%"
    ) else (
        echo [ERROR] AIVEN_MYSQL_CLIENT_PATH not found: "%AIVEN_MYSQL_CLIENT_PATH%"
        exit /b 1
    )
)

if "%MYSQL_CLIENT%"=="" (
    where mysql >nul 2>nul
    if not errorlevel 1 (
        set "MYSQL_CLIENT=mysql"
    )
)

if "%MYSQL_CLIENT%"=="" (
    if exist "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" (
        set "MYSQL_CLIENT=C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
    )
)

if "%MYSQL_CLIENT%"=="" (
    if exist "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" (
        set "MYSQL_CLIENT=C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe"
    )
)

if "%MYSQL_CLIENT%"=="" (
    echo [ERROR] mysql client not found.
    echo Please add mysql to PATH or set AIVEN_MYSQL_CLIENT_PATH in "%ENV_FILE%"
    exit /b 1
)

exit /b 0
