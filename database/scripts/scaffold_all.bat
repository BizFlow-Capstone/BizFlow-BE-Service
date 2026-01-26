@echo off
SETLOCAL EnableDelayedExpansion

echo ================================================
echo    SCAFFOLD ALL ENTITIES (Exclude History)
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

echo [INFO] Getting list of tables from database...
echo.

:: Lấy danh sách tất cả tables (trừ __MigrationHistory)
set TABLES_LIST=

:: Tạm thời lưu danh sách tables vào file
set TEMP_FILE=%TEMP%\temp_tables.txt
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -sN -e "SHOW TABLES;" 2>nul > "%TEMP_FILE%"

:: Đọc file và build danh sách tables
for /f "tokens=*" %%t in (%TEMP_FILE%) do (
    if not "%%t"=="__MigrationHistory" (
        echo [INFO] Will scaffold table: %%t
        set TABLES_LIST=!TABLES_LIST! --table %%t
    )
)

:: Xóa file tạm
del "%TEMP_FILE%" 2>nul

if "!TABLES_LIST!"=="" (
    echo [ERROR] No tables found in database!
    pause
    exit /b 1
)

echo.
set /p CONFIRM="Do you want to scaffold all these tables? (Y/N): "

if /i not "%CONFIRM%"=="Y" (
    echo.
    echo [CANCELLED] Scaffold cancelled by user.
    pause
    exit /b 0
)

echo.
echo [RUNNING] Scaffolding all entities...
echo.

:: Vào thư mục Infrastructure
cd /d "%~dp0..\..\bizflow-platform\BizFlow.Infrastructure"

:: Chạy scaffold command
dotnet ef dbcontext scaffold "Server=localhost;Port=3307;Database=bizflow_db;User=admin;Password=admin;CharSet=utf8mb4;SslMode=none;" Pomelo.EntityFrameworkCore.MySql --context-dir ./Data --output-dir ../BizFlow.Domain/Entities --context BizFlowDbContext --force --no-onconfiguring !TABLES_LIST!

if errorlevel 1 (
    echo.
    echo [ERROR] Scaffold failed!
    pause
    exit /b 1
)

echo.
echo ================================================
echo    ALL ENTITIES SCAFFOLDED SUCCESSFULLY!
echo ================================================
echo.
pause