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

:: Tables to scaffold (plural names in database)
set "TABLES=--table Roles --table Accounts --table Profiles --table Credentials --table RefreshTokens --table BusinessTypes --table BusinessTypeTaxes --table BusinessLocations --table UserLocationAssignments --table Products --table ProductPricePolicies --table Imports --table ProductsImports --table SaleItems --table Hires --table ImportSchemas --table ImportSchemaVersions --table StockMovements --table SystemConfig"

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
    --no-pluralize ^
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
:: STEP 4: RENAME ENTITY FILES (Plural -> Singular)
:: ============================================
echo [STEP 4] Renaming entity files to singular...

:: Rename files from plural to singular
if exist "%TEMP_ENTITIES_DIR%\Roles.cs" (
    ren "%TEMP_ENTITIES_DIR%\Roles.cs" "Role.cs"
    echo   [RENAME] Roles.cs -^> Role.cs
)
if exist "%TEMP_ENTITIES_DIR%\BusinessTypes.cs" (
    ren "%TEMP_ENTITIES_DIR%\BusinessTypes.cs" "BusinessType.cs"
    echo   [RENAME] BusinessTypes.cs -^> BusinessType.cs
)
if exist "%TEMP_ENTITIES_DIR%\BusinessTypeTaxes.cs" (
    ren "%TEMP_ENTITIES_DIR%\BusinessTypeTaxes.cs" "BusinessTypeTax.cs"
    echo   [RENAME] BusinessTypeTaxes.cs -^> BusinessTypeTax.cs
)
if exist "%TEMP_ENTITIES_DIR%\BusinessLocations.cs" (
    ren "%TEMP_ENTITIES_DIR%\BusinessLocations.cs" "BusinessLocation.cs"
    echo   [RENAME] BusinessLocations.cs -^> BusinessLocation.cs
)
if exist "%TEMP_ENTITIES_DIR%\UserLocationAssignments.cs" (
    ren "%TEMP_ENTITIES_DIR%\UserLocationAssignments.cs" "UserLocationAssignment.cs"
    echo   [RENAME] UserLocationAssignments.cs -^> UserLocationAssignment.cs
)
if exist "%TEMP_ENTITIES_DIR%\Products.cs" (
    ren "%TEMP_ENTITIES_DIR%\Products.cs" "Product.cs"
    echo   [RENAME] Products.cs -^> Product.cs
)
if exist "%TEMP_ENTITIES_DIR%\SaleItems.cs" (
    ren "%TEMP_ENTITIES_DIR%\SaleItems.cs" "SaleItem.cs"
    echo   [RENAME] SaleItems.cs -^> SaleItem.cs
)
if exist "%TEMP_ENTITIES_DIR%\ProductPricePolicies.cs" (
    ren "%TEMP_ENTITIES_DIR%\ProductPricePolicies.cs" "ProductPricePolicy.cs"
    echo   [RENAME] ProductPricePolicies.cs -^> ProductPricePolicy.cs
)
if exist "%TEMP_ENTITIES_DIR%\Imports.cs" (
    ren "%TEMP_ENTITIES_DIR%\Imports.cs" "Import.cs"
    echo   [RENAME] Imports.cs -^> Import.cs
)
if exist "%TEMP_ENTITIES_DIR%\ProductsImports.cs" (
    ren "%TEMP_ENTITIES_DIR%\ProductsImports.cs" "ProductImport.cs"
    echo   [RENAME] ProductsImports.cs -^> ProductImport.cs
)
if exist "%TEMP_ENTITIES_DIR%\Hires.cs" (
    ren "%TEMP_ENTITIES_DIR%\Hires.cs" "Hire.cs"
    echo   [RENAME] Hires.cs -^> Hire.cs
)
if exist "%TEMP_ENTITIES_DIR%\Accounts.cs" (
    ren "%TEMP_ENTITIES_DIR%\Accounts.cs" "Account.cs"
    echo   [RENAME] Accounts.cs -^> Account.cs
)
if exist "%TEMP_ENTITIES_DIR%\Profiles.cs" (
    ren "%TEMP_ENTITIES_DIR%\Profiles.cs" "Profile.cs"
    echo   [RENAME] Profiles.cs -^> Profile.cs
)
if exist "%TEMP_ENTITIES_DIR%\Credentials.cs" (
    ren "%TEMP_ENTITIES_DIR%\Credentials.cs" "Credential.cs"
    echo   [RENAME] Credentials.cs -^> Credential.cs
)
if exist "%TEMP_ENTITIES_DIR%\RefreshTokens.cs" (
    ren "%TEMP_ENTITIES_DIR%\RefreshTokens.cs" "RefreshToken.cs"
    echo   [RENAME] RefreshTokens.cs -^> RefreshToken.cs
)
if exist "%TEMP_ENTITIES_DIR%\ImportSchemas.cs" (
    ren "%TEMP_ENTITIES_DIR%\ImportSchemas.cs" "ImportSchema.cs"
    echo   [RENAME] ImportSchemas.cs -^> ImportSchema.cs
)
if exist "%TEMP_ENTITIES_DIR%\ImportSchemaVersions.cs" (
    ren "%TEMP_ENTITIES_DIR%\ImportSchemaVersions.cs" "ImportSchemaVersion.cs"
    echo   [RENAME] ImportSchemaVersions.cs -^> ImportSchemaVersion.cs
)
if exist "%TEMP_ENTITIES_DIR%\StockMovements.cs" (
    ren "%TEMP_ENTITIES_DIR%\StockMovements.cs" "StockMovement.cs"
    echo   [RENAME] StockMovements.cs -^> StockMovement.cs
)
if exist "%TEMP_ENTITIES_DIR%\SystemConfig.cs" (
    echo   [SKIP] SystemConfig.cs already singular
)
echo.

:: ============================================
:: STEP 5: FIX ENTITY NAMESPACES AND CLASS NAMES
:: ============================================
echo [STEP 5] Moving and fixing entity files...

:: Create Entities directory if not exists
if not exist "%FINAL_ENTITIES_DIR%" mkdir "%FINAL_ENTITIES_DIR%"

set ENTITY_COUNT=0

:: Process each .cs file in TempEntities
for %%F in ("%TEMP_ENTITIES_DIR%\*.cs") do (
    set "INPUT_FILE=%%F"
    set "FILENAME=%%~nxF"
    set "OUTPUT_FILE=%FINAL_ENTITIES_DIR%\%%~nxF"
    
    :: Replace namespace and class names using PowerShell
    powershell -Command "$content = Get-Content '!INPUT_FILE!' -Raw; $content = $content -replace 'namespace BizFlow\.Infrastructure\.TempEntities', 'namespace BizFlow.Domain.Entities' -replace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', '' -replace 'public partial class Roles', 'public partial class Role' -replace 'public partial class Accounts', 'public partial class Account' -replace 'public partial class Profiles', 'public partial class Profile' -replace 'public partial class BusinessTypes', 'public partial class BusinessType' -replace 'public partial class BusinessTypeTaxes', 'public partial class BusinessTypeTax' -replace 'public partial class BusinessLocations', 'public partial class BusinessLocation' -replace 'public partial class UserLocationAssignments', 'public partial class UserLocationAssignment' -replace 'public partial class ProductsImports', 'public partial class ProductImport' -replace 'public partial class ProductImports', 'public partial class ProductImport' -replace 'public partial class ProductPricePolicies', 'public partial class ProductPricePolicy' -replace 'public partial class Products', 'public partial class Product' -replace 'public partial class SaleItems', 'public partial class SaleItem' -replace 'public partial class ImportSchemaVersions', 'public partial class ImportSchemaVersion' -replace 'public partial class ImportSchemas', 'public partial class ImportSchema' -replace 'public partial class Imports', 'public partial class Import' -replace 'public partial class Hires', 'public partial class Hire' -replace 'public partial class StockMovements', 'public partial class StockMovement' -replace 'ICollection<ImportSchemaVersions>', 'ICollection<ImportSchemaVersion>' -replace 'ICollection<ImportSchemas>', 'ICollection<ImportSchema>' -replace 'ICollection<Imports>', 'ICollection<Import>' -replace 'ICollection<Roles>', 'ICollection<Role>' -replace 'ICollection<Accounts>', 'ICollection<Account>' -replace 'ICollection<Profiles>', 'ICollection<Profile>' -replace 'ICollection<BusinessTypes>', 'ICollection<BusinessType>' -replace 'ICollection<BusinessTypeTaxes>', 'ICollection<BusinessTypeTax>' -replace 'ICollection<BusinessLocations>', 'ICollection<BusinessLocation>' -replace 'ICollection<UserLocationAssignments>', 'ICollection<UserLocationAssignment>' -replace 'ICollection<ProductsImports>', 'ICollection<ProductImport>' -replace 'ICollection<ProductImports>', 'ICollection<ProductImport>' -replace 'ICollection<ProductPricePolicies>', 'ICollection<ProductPricePolicy>' -replace 'ICollection<Products>', 'ICollection<Product>' -replace 'ICollection<SaleItems>', 'ICollection<SaleItem>' -replace 'ICollection<Hires>', 'ICollection<Hire>' -replace 'ICollection<StockMovements>', 'ICollection<StockMovement>' -replace 'List<ImportSchemaVersions>', 'List<ImportSchemaVersion>' -replace 'List<ImportSchemas>', 'List<ImportSchema>' -replace 'List<Imports>', 'List<Import>' -replace 'List<Roles>', 'List<Role>' -replace 'List<Accounts>', 'List<Account>' -replace 'List<Profiles>', 'List<Profile>' -replace 'List<BusinessTypes>', 'List<BusinessType>' -replace 'List<BusinessTypeTaxes>', 'List<BusinessTypeTax>' -replace 'List<BusinessLocations>', 'List<BusinessLocation>' -replace 'List<UserLocationAssignments>', 'List<UserLocationAssignment>' -replace 'List<ProductsImports>', 'List<ProductImport>' -replace 'List<ProductImports>', 'List<ProductImport>' -replace 'List<ProductPricePolicies>', 'List<ProductPricePolicy>' -replace 'List<Products>', 'List<Product>' -replace 'List<SaleItems>', 'List<SaleItem>' -replace 'List<Hires>', 'List<Hire>' -replace 'List<StockMovements>', 'List<StockMovement>' -replace 'virtual ImportSchemaVersions', 'virtual ImportSchemaVersion' -replace 'virtual ImportSchemas', 'virtual ImportSchema' -replace 'virtual Imports', 'virtual Import' -replace 'virtual Roles', 'virtual Role' -replace 'virtual Accounts', 'virtual Account' -replace 'virtual Profiles', 'virtual Profile' -replace 'virtual BusinessTypes', 'virtual BusinessType' -replace 'virtual BusinessTypeTaxes', 'virtual BusinessTypeTax' -replace 'virtual BusinessLocations', 'virtual BusinessLocation' -replace 'virtual UserLocationAssignments', 'virtual UserLocationAssignment' -replace 'virtual ProductsImports', 'virtual ProductImport' -replace 'virtual ProductImports', 'virtual ProductImport' -replace 'virtual ProductPricePolicies', 'virtual ProductPricePolicy' -replace 'virtual Products', 'virtual Product' -replace 'virtual SaleItems', 'virtual SaleItem' -replace 'virtual Hires', 'virtual Hire' -replace 'public partial class Credentials', 'public partial class Credential' -replace 'public partial class RefreshTokens', 'public partial class RefreshToken' -replace 'ICollection<Credentials>', 'ICollection<Credential>' -replace 'ICollection<RefreshTokens>', 'ICollection<RefreshToken>' -replace 'List<Credentials>', 'List<Credential>' -replace 'List<RefreshTokens>', 'List<RefreshToken>' -replace 'virtual Credentials', 'virtual Credential' -replace 'virtual RefreshTokens', 'virtual RefreshToken'; Set-Content '!OUTPUT_FILE!' -Value $content -NoNewline"
    
    set /a ENTITY_COUNT+=1
    echo   [OK] Fixed: !FILENAME!
)

echo   Total entities: !ENTITY_COUNT!
echo.

:: ============================================
:: STEP 6: FIX DBCONTEXT
:: ============================================
echo [STEP 6] Moving and fixing DbContext...

:: Create DataContext directory if not exists
if not exist "%FINAL_DATACONTEXT_DIR%" mkdir "%FINAL_DATACONTEXT_DIR%"

set "DBCONTEXT_INPUT=%TEMP_DATA_DIR%\BizFlowDbContext.cs"
set "DBCONTEXT_OUTPUT=%FINAL_DATACONTEXT_DIR%\BizFlowDbContext.cs"

if exist "%DBCONTEXT_INPUT%" (
    :: Fix DbContext namespace, add Entity using, and update DbSet types
    powershell -Command "$content = Get-Content '%DBCONTEXT_INPUT%' -Raw; $content = $content -replace 'namespace BizFlow\.Infrastructure\.TempData', 'namespace BizFlow.Infrastructure.DataContext'; if ($content -notmatch 'using BizFlow\.Domain\.Entities;') { $content = $content -replace '(using .*?;\r?\n)', \"`$1using BizFlow.Domain.Entities;`r`n\" }; $content = $content -replace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', '' -replace 'DbSet<Roles>', 'DbSet<Role>' -replace 'DbSet<Accounts>', 'DbSet<Account>' -replace 'DbSet<Profiles>', 'DbSet<Profile>' -replace 'DbSet<BusinessTypes>', 'DbSet<BusinessType>' -replace 'DbSet<BusinessTypeTaxes>', 'DbSet<BusinessTypeTax>' -replace 'DbSet<BusinessLocations>', 'DbSet<BusinessLocation>' -replace 'DbSet<UserLocationAssignments>', 'DbSet<UserLocationAssignment>' -replace 'DbSet<Products>', 'DbSet<Product>' -replace 'DbSet<SaleItems>', 'DbSet<SaleItem>' -replace 'DbSet<ProductPricePolicies>', 'DbSet<ProductPricePolicy>' -replace 'DbSet<Imports>', 'DbSet<Import>' -replace 'DbSet<ProductsImports>', 'DbSet<ProductImport>' -replace 'DbSet<ProductImports>', 'DbSet<ProductImport>' -replace 'DbSet<Hires>', 'DbSet<Hire>' -replace 'DbSet<ImportSchemas>', 'DbSet<ImportSchema>' -replace 'DbSet<ImportSchemaVersions>', 'DbSet<ImportSchemaVersion>' -replace 'DbSet<StockMovements>', 'DbSet<StockMovement>' -replace 'DbSet<Credentials>', 'DbSet<Credential>' -replace 'DbSet<RefreshTokens>', 'DbSet<RefreshToken>' -replace 'modelBuilder\.Entity<Roles>', 'modelBuilder.Entity<Role>' -replace 'modelBuilder\.Entity<Accounts>', 'modelBuilder.Entity<Account>' -replace 'modelBuilder\.Entity<Profiles>', 'modelBuilder.Entity<Profile>' -replace 'modelBuilder\.Entity<BusinessTypes>', 'modelBuilder.Entity<BusinessType>' -replace 'modelBuilder\.Entity<BusinessTypeTaxes>', 'modelBuilder.Entity<BusinessTypeTax>' -replace 'modelBuilder\.Entity<BusinessLocations>', 'modelBuilder.Entity<BusinessLocation>' -replace 'modelBuilder\.Entity<UserLocationAssignments>', 'modelBuilder.Entity<UserLocationAssignment>' -replace 'modelBuilder\.Entity<Products>', 'modelBuilder.Entity<Product>' -replace 'modelBuilder\.Entity<SaleItems>', 'modelBuilder.Entity<SaleItem>' -replace 'modelBuilder\.Entity<ProductPricePolicies>', 'modelBuilder.Entity<ProductPricePolicy>' -replace 'modelBuilder\.Entity<Imports>', 'modelBuilder.Entity<Import>' -replace 'modelBuilder\.Entity<ProductsImports>', 'modelBuilder.Entity<ProductImport>' -replace 'modelBuilder\.Entity<ProductImports>', 'modelBuilder.Entity<ProductImport>' -replace 'modelBuilder\.Entity<Hires>', 'modelBuilder.Entity<Hire>' -replace 'modelBuilder\.Entity<ImportSchemas>', 'modelBuilder.Entity<ImportSchema>' -replace 'modelBuilder\.Entity<ImportSchemaVersions>', 'modelBuilder.Entity<ImportSchemaVersion>' -replace 'modelBuilder\.Entity<StockMovements>', 'modelBuilder.Entity<StockMovement>' -replace 'modelBuilder\.Entity<Credentials>', 'modelBuilder.Entity<Credential>' -replace 'modelBuilder\.Entity<RefreshTokens>', 'modelBuilder.Entity<RefreshToken>'; Set-Content '%DBCONTEXT_OUTPUT%' -Value $content -NoNewline"
    
    echo   [OK] Fixed: BizFlowDbContext.cs
) else (
    echo   [WARNING] DbContext not found in TempData
)
echo.

:: ============================================
:: STEP 7: CLEANUP TEMP DIRECTORIES
:: ============================================
echo [STEP 7] Cleaning up temporary files...

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
echo [TRANSFORMATIONS]
echo   - Database tables (plural) -^> Entity classes (singular)
echo   - Example: BusinessLocations table -^> BusinessLocation class
echo.
echo [NEXT STEPS]
echo   1. Review generated files
echo   2. Build solution: dotnet build
echo   3. Run application: dotnet run
echo.
echo ================================================
echo.
pause
