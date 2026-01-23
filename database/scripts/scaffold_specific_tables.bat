@echo off
SETLOCAL EnableDelayedExpansion

echo ================================================
echo    SCAFFOLD SPECIFIC TABLES
echo ================================================
echo.

:: Kiểm tra Docker container
docker ps | findstr bizflow-mysql >nul
if errorlevel 1 (
    echo [ERROR] MySQL container is not running!
    echo Please start Docker container first: docker-compose up -d
    pause
    exit /b 1
)

echo [INFO] Available tables in database:
echo.

:: Hiển thị danh sách tables
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -e "SHOW TABLES;" -t

echo.
echo [INPUT] Enter table names separated by space
echo Example: Roles Users UserRoles
echo.
set /p TABLES="Table names: "

if "%TABLES%"=="" (
    echo [ERROR] No tables specified!
    pause
    exit /b 1
)

echo.
echo [INFO] Will scaffold these tables: %TABLES%
echo.
set /p CONFIRM="Continue? (Y/N): "

if /i not "%CONFIRM%"=="Y" (
    echo [CANCELLED] Scaffold cancelled.
    pause
    exit /b 0
)

:: Build scaffold command
set SCAFFOLD_CMD=dotnet ef dbcontext scaffold "Server=localhost;Port=3307;Database=bizflow_db;User=admin;Password=admin;CharSet=utf8mb4;SslMode=none;" Pomelo.EntityFrameworkCore.MySql --context-dir ./Data --output-dir ../BizFlow.Domain/Entities --context BizFlowDbContext --force --no-onconfiguring --no-pluralize

:: Thêm từng table
for %%t in (%TABLES%) do (
    set SCAFFOLD_CMD=!SCAFFOLD_CMD! --table %%t
)

echo.
echo [RUNNING] Scaffolding entities...
echo.

:: Vào thư mục Infrastructure
cd /d "%~dp0..\..\bizflow-platform\BizFlow.Infrastructure"

:: Chạy scaffold
%SCAFFOLD_CMD%

if errorlevel 1 (
    echo.
    echo [ERROR] Scaffold failed!
    pause
    exit /b 1
)

echo.
echo ================================================
echo    ENTITIES SCAFFOLDED SUCCESSFULLY!
echo ================================================
echo.
pause