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
set "SOLUTION_FILE=%PROJECT_ROOT%\bizflow-platform\bizflow-platform.sln"
set "TEMP_ENTITIES_DIR=%INFRASTRUCTURE_DIR%\TempEntities"
set "TEMP_DATA_DIR=%INFRASTRUCTURE_DIR%\TempData"
set "FINAL_ENTITIES_DIR=%DOMAIN_DIR%\Entities"
set "FINAL_DATACONTEXT_DIR=%INFRASTRUCTURE_DIR%\DataContext"

:: Tables to scaffold (plural names in database)
:: NOTE: AccountingPeriods/AccountingPeriodAuditLogs are configured in BizFlowDbContext.Custom.cs
:: and intentionally excluded here to avoid duplicate DbSet/ModelBuilder definitions.
set "TABLES=--table Roles --table Accounts --table Profiles --table Credentials --table RefreshTokens --table DeviceTokens --table BusinessTypes --table BusinessTypeTaxes --table BusinessLocations --table UserLocationAssignments --table Products --table ProductPricePolicies --table Imports --table ProductsImports --table SaleItems --table Hires --table ImportSchemas --table ImportSchemaVersions --table StockMovements --table SystemConfig --table Debtors --table DebtorPaymentTransactions --table Orders --table OrderDetails --table Revenues --table Costs --table GeneralLedgerEntries"

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
if exist "%TEMP_ENTITIES_DIR%\DeviceTokens.cs" (
    ren "%TEMP_ENTITIES_DIR%\DeviceTokens.cs" "DeviceToken.cs"
    echo   [RENAME] DeviceTokens.cs -^> DeviceToken.cs
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
if exist "%TEMP_ENTITIES_DIR%\Debtors.cs" (
    ren "%TEMP_ENTITIES_DIR%\Debtors.cs" "Debtor.cs"
    echo   [RENAME] Debtors.cs -^> Debtor.cs
)
if exist "%TEMP_ENTITIES_DIR%\DebtorPaymentTransactions.cs" (
    ren "%TEMP_ENTITIES_DIR%\DebtorPaymentTransactions.cs" "DebtorPaymentTransaction.cs"
    echo   [RENAME] DebtorPaymentTransactions.cs -^> DebtorPaymentTransaction.cs
)
if exist "%TEMP_ENTITIES_DIR%\Orders.cs" (
    ren "%TEMP_ENTITIES_DIR%\Orders.cs" "Order.cs"
    echo   [RENAME] Orders.cs -^> Order.cs
)
if exist "%TEMP_ENTITIES_DIR%\OrderDetails.cs" (
    ren "%TEMP_ENTITIES_DIR%\OrderDetails.cs" "OrderDetail.cs"
    echo   [RENAME] OrderDetails.cs -^> OrderDetail.cs
)
if exist "%TEMP_ENTITIES_DIR%\Revenues.cs" (
    ren "%TEMP_ENTITIES_DIR%\Revenues.cs" "Revenue.cs"
    echo   [RENAME] Revenues.cs -^> Revenue.cs
)
if exist "%TEMP_ENTITIES_DIR%\Costs.cs" (
    ren "%TEMP_ENTITIES_DIR%\Costs.cs" "Cost.cs"
    echo   [RENAME] Costs.cs -^> Cost.cs
)
if exist "%TEMP_ENTITIES_DIR%\GeneralLedgerEntries.cs" (
    ren "%TEMP_ENTITIES_DIR%\GeneralLedgerEntries.cs" "GeneralLedgerEntry.cs"
    echo   [RENAME] GeneralLedgerEntries.cs -^> GeneralLedgerEntry.cs
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
    powershell -Command "$content = Get-Content '!INPUT_FILE!' -Raw; $content = $content -creplace 'namespace BizFlow\.Infrastructure\.TempEntities', 'namespace BizFlow.Domain.Entities' -creplace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', '' -creplace 'public partial class Roles', 'public partial class Role' -creplace 'public partial class Accounts', 'public partial class Account' -creplace 'public partial class Profiles', 'public partial class Profile' -creplace 'public partial class BusinessTypes', 'public partial class BusinessType' -creplace 'public partial class BusinessTypeTaxes', 'public partial class BusinessTypeTax' -creplace 'public partial class BusinessLocations', 'public partial class BusinessLocation' -creplace 'public partial class UserLocationAssignments', 'public partial class UserLocationAssignment' -creplace 'public partial class ProductsImports', 'public partial class ProductImport' -creplace 'public partial class ProductImports', 'public partial class ProductImport' -creplace 'public partial class ProductPricePolicies', 'public partial class ProductPricePolicy' -creplace 'public partial class Products', 'public partial class Product' -creplace 'public partial class SaleItems', 'public partial class SaleItem' -creplace 'public partial class ImportSchemaVersions', 'public partial class ImportSchemaVersion' -creplace 'public partial class ImportSchemas', 'public partial class ImportSchema' -creplace 'public partial class Imports', 'public partial class Import' -creplace 'public partial class Hires', 'public partial class Hire' -creplace 'public partial class StockMovements', 'public partial class StockMovement' -creplace 'ICollection<ImportSchemaVersions>', 'ICollection<ImportSchemaVersion>' -creplace 'ICollection<ImportSchemas>', 'ICollection<ImportSchema>' -creplace 'ICollection<Imports>', 'ICollection<Import>' -creplace 'ICollection<Roles>', 'ICollection<Role>' -creplace 'ICollection<Accounts>', 'ICollection<Account>' -creplace 'ICollection<Profiles>', 'ICollection<Profile>' -creplace 'ICollection<BusinessTypes>', 'ICollection<BusinessType>' -creplace 'ICollection<BusinessTypeTaxes>', 'ICollection<BusinessTypeTax>' -creplace 'ICollection<BusinessLocations>', 'ICollection<BusinessLocation>' -creplace 'ICollection<UserLocationAssignments>', 'ICollection<UserLocationAssignment>' -creplace 'ICollection<ProductsImports>', 'ICollection<ProductImport>' -creplace 'ICollection<ProductImports>', 'ICollection<ProductImport>' -creplace 'ICollection<ProductPricePolicies>', 'ICollection<ProductPricePolicy>' -creplace 'ICollection<Products>', 'ICollection<Product>' -creplace 'ICollection<SaleItems>', 'ICollection<SaleItem>' -creplace 'ICollection<Hires>', 'ICollection<Hire>' -creplace 'ICollection<StockMovements>', 'ICollection<StockMovement>' -creplace 'List<ImportSchemaVersions>', 'List<ImportSchemaVersion>' -creplace 'List<ImportSchemas>', 'List<ImportSchema>' -creplace 'List<Imports>', 'List<Import>' -creplace 'List<Roles>', 'List<Role>' -creplace 'List<Accounts>', 'List<Account>' -creplace 'List<Profiles>', 'List<Profile>' -creplace 'List<BusinessTypes>', 'List<BusinessType>' -creplace 'List<BusinessTypeTaxes>', 'List<BusinessTypeTax>' -creplace 'List<BusinessLocations>', 'List<BusinessLocation>' -creplace 'List<UserLocationAssignments>', 'List<UserLocationAssignment>' -creplace 'List<ProductsImports>', 'List<ProductImport>' -creplace 'List<ProductImports>', 'List<ProductImport>' -creplace 'List<ProductPricePolicies>', 'List<ProductPricePolicy>' -creplace 'List<Products>', 'List<Product>' -creplace 'List<SaleItems>', 'List<SaleItem>' -creplace 'List<Hires>', 'List<Hire>' -creplace 'List<StockMovements>', 'List<StockMovement>' -creplace 'virtual ImportSchemaVersions', 'virtual ImportSchemaVersion' -creplace 'virtual ImportSchemas', 'virtual ImportSchema' -creplace 'virtual Imports', 'virtual Import' -creplace 'virtual Roles', 'virtual Role' -creplace 'virtual Accounts', 'virtual Account' -creplace 'virtual Profiles', 'virtual Profile' -creplace 'virtual BusinessTypes', 'virtual BusinessType' -creplace 'virtual BusinessTypeTaxes', 'virtual BusinessTypeTax' -creplace 'virtual BusinessLocations', 'virtual BusinessLocation' -creplace 'virtual UserLocationAssignments', 'virtual UserLocationAssignment' -creplace 'virtual ProductsImports', 'virtual ProductImport' -creplace 'virtual ProductImports', 'virtual ProductImport' -creplace 'virtual ProductPricePolicies', 'virtual ProductPricePolicy' -creplace 'virtual Products', 'virtual Product' -creplace 'virtual SaleItems', 'virtual SaleItem' -creplace 'virtual Hires', 'virtual Hire' -creplace 'public partial class Credentials', 'public partial class Credential' -creplace 'public partial class RefreshTokens', 'public partial class RefreshToken' -creplace 'ICollection<Credentials>', 'ICollection<Credential>' -creplace 'ICollection<RefreshTokens>', 'ICollection<RefreshToken>' -creplace 'List<Credentials>', 'List<Credential>' -creplace 'List<RefreshTokens>', 'List<RefreshToken>' -creplace 'virtual Credentials', 'virtual Credential' -creplace 'virtual RefreshTokens', 'virtual RefreshToken' -creplace 'Profile\? Profiles', 'Profile? Profile' -creplace 'Profile Profiles', 'Profile Profile' -creplace 'public partial class Debtors', 'public partial class Debtor' -creplace 'public partial class DebtorPaymentTransactions', 'public partial class DebtorPaymentTransaction' -creplace 'public partial class Orders', 'public partial class Order' -creplace 'public partial class OrderDetails', 'public partial class OrderDetail' -creplace 'public partial class Revenues', 'public partial class Revenue' -creplace 'public partial class Costs', 'public partial class Cost' -creplace 'public partial class GeneralLedgerEntries', 'public partial class GeneralLedgerEntry' -creplace 'ICollection<Debtors>', 'ICollection<Debtor>' -creplace 'ICollection<DebtorPaymentTransactions>', 'ICollection<DebtorPaymentTransaction>' -creplace 'ICollection<Orders>', 'ICollection<Order>' -creplace 'ICollection<OrderDetails>', 'ICollection<OrderDetail>' -creplace 'ICollection<Revenues>', 'ICollection<Revenue>' -creplace 'ICollection<Costs>', 'ICollection<Cost>' -creplace 'ICollection<GeneralLedgerEntries>', 'ICollection<GeneralLedgerEntry>' -creplace 'List<Debtors>', 'List<Debtor>' -creplace 'List<DebtorPaymentTransactions>', 'List<DebtorPaymentTransaction>' -creplace 'List<Orders>', 'List<Order>' -creplace 'List<OrderDetails>', 'List<OrderDetail>' -creplace 'List<Revenues>', 'List<Revenue>' -creplace 'List<Costs>', 'List<Cost>' -creplace 'List<GeneralLedgerEntries>', 'List<GeneralLedgerEntry>' -creplace 'virtual Debtors', 'virtual Debtor' -creplace 'virtual DebtorPaymentTransactions', 'virtual DebtorPaymentTransaction' -creplace 'virtual Orders\b', 'virtual Order' -creplace 'virtual OrderDetails', 'virtual OrderDetail' -creplace 'virtual Revenues', 'virtual Revenue' -creplace 'virtual Costs\b', 'virtual Cost' -creplace 'virtual GeneralLedgerEntries', 'virtual GeneralLedgerEntry'; Set-Content '!OUTPUT_FILE!' -Value $content -NoNewline"

    for %%Z in ("!OUTPUT_FILE!") do (
        if %%~zZ EQU 0 (
            echo   [WARNING] Empty output detected for !FILENAME!, fallback to raw scaffold content
            powershell -Command "$raw = Get-Content '!INPUT_FILE!' -Raw; $raw = $raw -creplace 'namespace BizFlow\.Infrastructure\.TempEntities', 'namespace BizFlow.Domain.Entities' -creplace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', ''; Set-Content '!OUTPUT_FILE!' -Value $raw -NoNewline"
        )
    )
    
    set /a ENTITY_COUNT+=1
    echo   [OK] Fixed: !FILENAME!
)

echo   Total entities: !ENTITY_COUNT!
echo.

:: ============================================
:: STEP 5.5: FIX DEVICE TOKEN NAMING
:: ============================================
echo [STEP 5.5] Fixing DeviceToken naming...

powershell -Command "$targets = @(); if (Test-Path '%FINAL_ENTITIES_DIR%') { $targets += Get-ChildItem '%FINAL_ENTITIES_DIR%' -Filter '*.cs' -File }; foreach ($file in $targets) { $content = Get-Content $file.FullName -Raw; $content = $content -creplace 'public partial class DeviceTokens', 'public partial class DeviceToken' -creplace 'ICollection<DeviceTokens>', 'ICollection<DeviceToken>' -creplace 'List<DeviceTokens>', 'List<DeviceToken>' -creplace 'virtual DeviceTokens', 'virtual DeviceToken'; Set-Content $file.FullName -Value $content -NoNewline }"

if errorlevel 1 (
    echo   [WARNING] Failed to patch DeviceToken names in entities.
) else (
    echo   [OK] DeviceToken names patched in entities
)

powershell -Command "$file = '%FINAL_ENTITIES_DIR%\BusinessLocation.cs'; if (Test-Path $file) { $content = Get-Content $file -Raw; if ($content -notmatch 'ICollection<AccountingPeriod>\s+AccountingPeriods') { $insert = '    public virtual ICollection<AccountingPeriod> AccountingPeriods { get; set; } = new List<AccountingPeriod>();`r`n`r`n'; $content = $content -replace '(public string\? TaxCode \{ get; set; \}\r?\n\r?\n)', ('$1' + $insert); Set-Content $file -Value $content -NoNewline } }"

if errorlevel 1 (
    echo   [WARNING] Failed to patch BusinessLocation.AccountingPeriods navigation.
) else (
    echo   [OK] BusinessLocation.AccountingPeriods navigation ensured
)
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
    powershell -Command "$content = Get-Content '%DBCONTEXT_INPUT%' -Raw; $content = $content -creplace 'namespace BizFlow\.Infrastructure\.TempData', 'namespace BizFlow.Infrastructure.DataContext'; if ($content -notmatch 'using BizFlow\.Domain\.Entities;') { $content = $content -creplace '(using .*?;\r?\n)', \"`$1using BizFlow.Domain.Entities;`r`n\" }; $content = $content -creplace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', '' -creplace 'DbSet<Roles>', 'DbSet<Role>' -creplace 'DbSet<Accounts>', 'DbSet<Account>' -creplace 'DbSet<Profiles>', 'DbSet<Profile>' -creplace 'DbSet<BusinessTypes>', 'DbSet<BusinessType>' -creplace 'DbSet<BusinessTypeTaxes>', 'DbSet<BusinessTypeTax>' -creplace 'DbSet<BusinessLocations>', 'DbSet<BusinessLocation>' -creplace 'DbSet<UserLocationAssignments>', 'DbSet<UserLocationAssignment>' -creplace 'DbSet<Products>', 'DbSet<Product>' -creplace 'DbSet<SaleItems>', 'DbSet<SaleItem>' -creplace 'DbSet<ProductPricePolicies>', 'DbSet<ProductPricePolicy>' -creplace 'DbSet<Imports>', 'DbSet<Import>' -creplace 'DbSet<ProductsImports>', 'DbSet<ProductImport>' -creplace 'DbSet<ProductImports>', 'DbSet<ProductImport>' -creplace 'DbSet<Hires>', 'DbSet<Hire>' -creplace 'DbSet<ImportSchemas>', 'DbSet<ImportSchema>' -creplace 'DbSet<ImportSchemaVersions>', 'DbSet<ImportSchemaVersion>' -creplace 'DbSet<StockMovements>', 'DbSet<StockMovement>' -creplace 'DbSet<Credentials>', 'DbSet<Credential>' -creplace 'DbSet<RefreshTokens>', 'DbSet<RefreshToken>' -creplace 'modelBuilder\.Entity<Roles>', 'modelBuilder.Entity<Role>' -creplace 'modelBuilder\.Entity<Accounts>', 'modelBuilder.Entity<Account>' -creplace 'modelBuilder\.Entity<Profiles>', 'modelBuilder.Entity<Profile>' -creplace 'modelBuilder\.Entity<BusinessTypes>', 'modelBuilder.Entity<BusinessType>' -creplace 'modelBuilder\.Entity<BusinessTypeTaxes>', 'modelBuilder.Entity<BusinessTypeTax>' -creplace 'modelBuilder\.Entity<BusinessLocations>', 'modelBuilder.Entity<BusinessLocation>' -creplace 'modelBuilder\.Entity<UserLocationAssignments>', 'modelBuilder.Entity<UserLocationAssignment>' -creplace 'modelBuilder\.Entity<Products>', 'modelBuilder.Entity<Product>' -creplace 'modelBuilder\.Entity<SaleItems>', 'modelBuilder.Entity<SaleItem>' -creplace 'modelBuilder\.Entity<ProductPricePolicies>', 'modelBuilder.Entity<ProductPricePolicy>' -creplace 'modelBuilder\.Entity<Imports>', 'modelBuilder.Entity<Import>' -creplace 'modelBuilder\.Entity<ProductsImports>', 'modelBuilder.Entity<ProductImport>' -creplace 'modelBuilder\.Entity<ProductImports>', 'modelBuilder.Entity<ProductImport>' -creplace 'modelBuilder\.Entity<Hires>', 'modelBuilder.Entity<Hire>' -creplace 'modelBuilder\.Entity<ImportSchemas>', 'modelBuilder.Entity<ImportSchema>' -creplace 'modelBuilder\.Entity<ImportSchemaVersions>', 'modelBuilder.Entity<ImportSchemaVersion>' -creplace 'modelBuilder\.Entity<StockMovements>', 'modelBuilder.Entity<StockMovement>' -creplace 'modelBuilder\.Entity<Credentials>', 'modelBuilder.Entity<Credential>' -creplace 'modelBuilder\.Entity<RefreshTokens>', 'modelBuilder.Entity<RefreshToken>' -creplace 'modelBuilder\.Entity<Debtors>', 'modelBuilder.Entity<Debtor>' -creplace 'modelBuilder\.Entity<DebtorPaymentTransactions>', 'modelBuilder.Entity<DebtorPaymentTransaction>' -creplace 'modelBuilder\.Entity<Orders>', 'modelBuilder.Entity<Order>' -creplace 'modelBuilder\.Entity<OrderDetails>', 'modelBuilder.Entity<OrderDetail>' -creplace 'modelBuilder\.Entity<Revenues>', 'modelBuilder.Entity<Revenue>' -creplace 'modelBuilder\.Entity<Costs>', 'modelBuilder.Entity<Cost>' -creplace 'modelBuilder\.Entity<GeneralLedgerEntries>', 'modelBuilder.Entity<GeneralLedgerEntry>' -creplace 'DbSet<Debtors>', 'DbSet<Debtor>' -creplace 'DbSet<DebtorPaymentTransactions>', 'DbSet<DebtorPaymentTransaction>' -creplace 'DbSet<Orders>', 'DbSet<Order>' -creplace 'DbSet<OrderDetails>', 'DbSet<OrderDetail>' -creplace 'DbSet<Revenues>', 'DbSet<Revenue>' -creplace 'DbSet<Costs>', 'DbSet<Cost>' -creplace 'DbSet<GeneralLedgerEntries>', 'DbSet<GeneralLedgerEntry>' -creplace 'HasForeignKey<Profiles>', 'HasForeignKey<Profile>' -creplace 'WithOne\(p => p\.Profiles\)', 'WithOne(p => p.Profile)'; Set-Content '%DBCONTEXT_OUTPUT%' -Value $content -NoNewline"
    
    echo   [OK] Fixed: BizFlowDbContext.cs
) else (
    echo   [WARNING] DbContext not found in TempData
)

powershell -Command "if (Test-Path '%DBCONTEXT_OUTPUT%') { $content = Get-Content '%DBCONTEXT_OUTPUT%' -Raw; $content = $content -creplace 'DbSet<DeviceTokens>', 'DbSet<DeviceToken>' -creplace 'modelBuilder\.Entity<DeviceTokens>', 'modelBuilder.Entity<DeviceToken>'; Set-Content '%DBCONTEXT_OUTPUT%' -Value $content -NoNewline }"

if errorlevel 1 (
    echo   [WARNING] Failed to patch DeviceToken names in DbContext.
) else (
    echo   [OK] DeviceToken names patched in DbContext
)
echo.

:: ============================================
:: STEP 6.4: DEDUPLICATE USING DIRECTIVES
:: ============================================
echo [STEP 6.4] Deduplicating using directives in generated files...

powershell -Command "$targets = @(); if (Test-Path '%FINAL_ENTITIES_DIR%') { $targets += Get-ChildItem '%FINAL_ENTITIES_DIR%' -Filter '*.cs' -File }; if (Test-Path '%FINAL_DATACONTEXT_DIR%\BizFlowDbContext.cs') { $targets += Get-Item '%FINAL_DATACONTEXT_DIR%\BizFlowDbContext.cs' }; foreach ($file in $targets) { $lines = Get-Content $file.FullName; $out = New-Object System.Collections.Generic.List[string]; $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal); $inHeader = $true; foreach ($line in $lines) { if ($inHeader -and $line -match '^\s*using\s+[^;]+;\s*$') { $key = $line.Trim(); if ($seen.Add($key)) { $out.Add($line) }; continue }; if ($inHeader -and ($line.Trim().Length -eq 0 -or $line.TrimStart().StartsWith('//'))) { $out.Add($line); continue }; $inHeader = $false; $out.Add($line) }; Set-Content $file.FullName -Value $out }"

if errorlevel 1 (
    echo   [WARNING] Failed to deduplicate using directives.
) else (
    echo   [OK] Deduplicated using directives
)
echo.

:: ============================================
:: STEP 6.5: REMOVE UNUSED USING DIRECTIVES
:: ============================================
echo [STEP 6.5] Removing unused using directives...

if exist "%SOLUTION_FILE%" (
    cd /d "%PROJECT_ROOT%\bizflow-platform"
    dotnet format "%SOLUTION_FILE%" ^
        --include "%FINAL_ENTITIES_DIR%" "%FINAL_DATACONTEXT_DIR%\BizFlowDbContext.cs" ^
        --diagnostics IDE0005 ^
        --verbosity minimal >nul 2>&1

    if errorlevel 1 (
        echo   [WARNING] dotnet format failed or unavailable. Skipped unused using cleanup.
    ) else (
        echo   [OK] Removed unused using directives (IDE0005)
    )
) else (
    echo   [WARNING] Solution file not found. Skipped unused using cleanup.
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
