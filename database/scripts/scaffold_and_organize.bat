@echo off
setlocal enabledelayedexpansion

echo ================================================
echo    SCAFFOLD AND ORGANIZE ENTITIES
echo ================================================
echo.

:: ============================================
:: CONFIGURATION
:: ============================================
set "CONNECTION_STRING=Server=localhost;Port=3307;Database=bizflow_db;User=admin;Password=admin;CharSet=utf8mb4;SslMode=none;"
set "PROJECT_ROOT=%~dp0..\.."
set "INFRASTRUCTURE_DIR=%PROJECT_ROOT%\bizflow-platform\BizFlow.Infrastructure"
set "DOMAIN_DIR=%PROJECT_ROOT%\bizflow-platform\BizFlow.Domain"
set "TEMP_ENTITIES_DIR=%INFRASTRUCTURE_DIR%\TempEntities"
set "TEMP_DATA_DIR=%INFRASTRUCTURE_DIR%\TempData"
set "FINAL_ENTITIES_DIR=%DOMAIN_DIR%\Entities"
set "FINAL_DATACONTEXT_DIR=%INFRASTRUCTURE_DIR%\DataContext"

:: Tables to scaffold
set "TABLES=--table Roles --table User --table BusinessType --table BusinessTypeTax --table BusinessLocation --table UserLocationAssignment --table Product --table ProductPricePolicy --table Import --table Product_Import --table SaleItem --table Hire"

:: ============================================
:: STEP 1: CHECK PREREQUISITES
:: ============================================
echo [STEP 1] Checking prerequisites...

:: Check Docker
docker ps | findstr bizflow-mysql >nul 2>&1
if errorlevel 1 (
    echo [ERROR] MySQL container is not running!
    echo Please start Docker: docker-compose up -d
    pause
    exit /b 1
)
echo   [OK] MySQL container is running
echo.

:: ============================================
:: STEP 2: CLEAN TEMP DIRECTORIES
:: ============================================
echo [STEP 2] Cleaning temporary directories...

if exist "%TEMP_ENTITIES_DIR%" (
    rmdir /S /Q "%TEMP_ENTITIES_DIR%"
    echo   [OK] Removed old TempEntities
)

if exist "%TEMP_DATA_DIR%" (
    rmdir /S /Q "%TEMP_DATA_DIR%"
    echo   [OK] Removed old TempData
)
echo.

:: ============================================
:: STEP 3: SCAFFOLD FROM DATABASE
:: ============================================
echo [STEP 3] Scaffolding entities from database...
echo.

cd /d "%INFRASTRUCTURE_DIR%"

dotnet ef dbcontext scaffold "%CONNECTION_STRING%" Pomelo.EntityFrameworkCore.MySql ^
    --context-dir ./TempData ^
    --output-dir ./TempEntities ^
    --context BizFlowDbContext ^
    --force ^
    --no-onconfiguring ^
    %TABLES%

if errorlevel 1 (
    echo.
    echo [ERROR] Scaffold failed!
    pause
    exit /b 1
)

echo.
echo   [OK] Entities scaffolded successfully
echo.

:: ============================================
:: STEP 4: FIX ENTITY NAMESPACES
:: ============================================
echo [STEP 4] Moving and fixing entity files...

:: Create Entities directory if not exists
if not exist "%FINAL_ENTITIES_DIR%" mkdir "%FINAL_ENTITIES_DIR%"

set ENTITY_COUNT=0

:: Process each .cs file in TempEntities
for %%F in ("%TEMP_ENTITIES_DIR%\*.cs") do (
    set "INPUT_FILE=%%F"
    set "FILENAME=%%~nxF"
    set "OUTPUT_FILE=%FINAL_ENTITIES_DIR%\%%~nxF"
    
    :: Replace namespace using PowerShell one-liner
    powershell -Command "(Get-Content '!INPUT_FILE!' -Raw) -replace 'namespace BizFlow\.Infrastructure\.TempEntities;?', 'namespace BizFlow.Domain.Entities;' -replace 'namespace BizFlow\.Infrastructure;?', 'namespace BizFlow.Domain.Entities;' -replace 'using BizFlow\.Infrastructure\.TempEntities;?\r?\n', '' -replace 'using BizFlow\.Infrastructure;?\r?\n', '' | Set-Content '!OUTPUT_FILE!' -NoNewline"
    
    set /a ENTITY_COUNT+=1
    echo   [OK] Fixed: !FILENAME!
)

echo   Total entities: !ENTITY_COUNT!
echo.

:: ============================================
:: STEP 5: FIX DBCONTEXT
:: ============================================
echo [STEP 5] Moving and fixing DbContext...

:: Create DataContext directory if not exists
if not exist "%FINAL_DATACONTEXT_DIR%" mkdir "%FINAL_DATACONTEXT_DIR%"

set "DBCONTEXT_INPUT=%TEMP_DATA_DIR%\BizFlowDbContext.cs"
set "DBCONTEXT_OUTPUT=%FINAL_DATACONTEXT_DIR%\BizFlowDbContext.cs"

if exist "%DBCONTEXT_INPUT%" (
    :: Fix DbContext namespace and add Entity using
    powershell -Command "$content = Get-Content '%DBCONTEXT_INPUT%' -Raw; $content = $content -replace 'namespace BizFlow\.Infrastructure\.TempData;?', 'namespace BizFlow.Infrastructure.DataContext;' -replace 'namespace BizFlow\.Infrastructure\.Data;?', 'namespace BizFlow.Infrastructure.DataContext;'; if ($content -notmatch 'using BizFlow\.Domain\.Entities;') { $content = $content -replace '(using .*?;\r?\n)', \"`$1using BizFlow.Domain.Entities;`r`n\" }; $content = $content -replace 'using BizFlow\.Infrastructure\.TempEntities;?\r?\n', ''; Set-Content '%DBCONTEXT_OUTPUT%' -Value $content -NoNewline"
    
    echo   [OK] Fixed: BizFlowDbContext.cs
) else (
    echo   [WARNING] DbContext not found in TempData
)
echo.

:: ============================================
:: STEP 6: CLEANUP TEMP DIRECTORIES
:: ============================================
echo [STEP 6] Cleaning up temporary files...

if exist "%TEMP_ENTITIES_DIR%" (
    rmdir /S /Q "%TEMP_ENTITIES_DIR%"
    echo   [OK] Removed TempEntities
)

if exist "%TEMP_DATA_DIR%" (
    rmdir /S /Q "%TEMP_DATA_DIR%"
    echo   [OK] Removed TempData
)
echo.

:: ============================================
:: SUCCESS SUMMARY
:: ============================================
echo ================================================
echo    SUCCESS! ENTITIES SCAFFOLDED
echo ================================================
echo.
echo [GENERATED FILES]
echo   Entities: %FINAL_ENTITIES_DIR%
echo   DbContext: %FINAL_DATACONTEXT_DIR%\BizFlowDbContext.cs
echo.
echo [ENTITY COUNT]
echo   Total: !ENTITY_COUNT! entities
echo.
echo [NEXT STEPS]
echo   1. Review generated files
echo   2. Build solution: dotnet build
echo   3. Run application: dotnet run
echo.
echo ================================================
echo.
pause
