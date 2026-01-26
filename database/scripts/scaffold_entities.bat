@echo off
echo ================================================
echo    SCAFFOLD ENTITIES FROM DATABASE
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

echo [INFO] Scaffolding entities from database...
echo.

:: Vào thư mục Infrastructure
cd /d "%~dp0..\..\bizflow-platform\BizFlow.Infrastructure"

echo [STEP 1] Fetching latest database schema...
echo.

:: Scaffold - Loại trừ bảng __MigrationHistory
dotnet ef dbcontext scaffold ^
    "Server=localhost;Port=3307;Database=bizflow_db;User=admin;Password=admin;CharSet=utf8mb4;SslMode=none;" ^
    Pomelo.EntityFrameworkCore.MySql ^
    --context-dir ./Data ^
    --output-dir ../BizFlow.Domain/Entities ^
    --context BizFlowDbContext ^
    --force ^
    --no-onconfiguring ^
    --table Roles ^
    --table Users ^
    --table UserRoles

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
echo [INFO] Generated files:
echo   - BizFlow.Domain/Entities/*.cs
echo   - BizFlow.Infrastructure/Data/BizFlowDbContext.cs
echo.
echo [NEXT STEPS]
echo   1. Review the generated entities
echo   2. Build the solution
echo   3. Test your APIs
echo.
pause