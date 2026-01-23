@echo off
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
set TABLES=

:: Tạm thời lưu danh sách tables vào file
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -sN -e "SHOW TABLES;" > temp_tables.txt

:: Đọc file và build câu lệnh
set SCAFFOLD_CMD=dotnet ef dbcontext scaffold "Server=localhost;Port=3307;Database=bizflow_db;User=admin;Password=admin;CharSet=utf8mb4;SslMode=none;" Pomelo.EntityFrameworkCore.MySql --context-dir ./Data --output-dir ../BizFlow.Domain/Entities --context BizFlowDbContext --force --no-onconfiguring --no-pluralize

:: Thêm từng table (trừ __MigrationHistory)
for /f "tokens=*" %%t in (temp_tables.txt) do (
    if not "%%t"=="__MigrationHistory" (
        echo [INFO] Will scaffold table: %%t
        set SCAFFOLD_CMD=!SCAFFOLD_CMD! --table %%t
    )
)

:: Xóa file tạm
del temp_tables.txt

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
%SCAFFOLD_CMD%

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