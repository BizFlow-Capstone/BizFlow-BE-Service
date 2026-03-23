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
for %%I in ("%~dp0.") do set "SCRIPT_DIR=%%~fI"
set "PROJECT_ROOT=%SCRIPT_DIR%\..\.."
set "INFRASTRUCTURE_DIR=%PROJECT_ROOT%\bizflow-platform\BizFlow.Infrastructure"
set "DOMAIN_DIR=%PROJECT_ROOT%\bizflow-platform\BizFlow.Domain"
set "SOLUTION_FILE=%PROJECT_ROOT%\bizflow-platform\bizflow-platform.sln"
set "TEMP_ENTITIES_DIR=%INFRASTRUCTURE_DIR%\TempEntities"
set "TEMP_DATA_DIR=%INFRASTRUCTURE_DIR%\TempData"
set "FINAL_ENTITIES_DIR=%DOMAIN_DIR%\Entities"
set "FINAL_DATACONTEXT_DIR=%INFRASTRUCTURE_DIR%\DataContext"
set "HELPER_PS1=%SCRIPT_DIR%\transform_scaffold_output.ps1"

if not exist "%HELPER_PS1%" (
    set "HELPER_PS1=%PROJECT_ROOT%\database\scripts\transform_scaffold_output.ps1"
)

if not exist "%HELPER_PS1%" (
    echo [ERROR] Missing helper script: transform_scaffold_output.ps1
    echo Expected at:
    echo   %SCRIPT_DIR%\transform_scaffold_output.ps1
    echo   %PROJECT_ROOT%\database\scripts\transform_scaffold_output.ps1
    pause
    exit /b 1
)

:: Tables to scaffold (plural names in database)
:: NOTE: AccountingPeriods/AccountingPeriodAuditLogs are configured in BizFlowDbContext.Custom.cs
:: and intentionally excluded here to avoid duplicate DbSet/ModelBuilder definitions.
set "TABLES=--table Roles --table Accounts --table Profiles --table Credentials --table RefreshTokens --table DeviceTokens --table BusinessTypes --table BusinessTypeTaxes --table BusinessLocations --table UserLocationAssignments --table Products --table ProductPricePolicies --table Imports --table ProductsImports --table SaleItems --table Hires --table ImportSchemas --table ImportSchemaVersions --table StockMovements --table SystemConfig --table Debtors --table DebtorPaymentTransactions --table Orders --table OrderDetails --table Revenues --table Costs --table GeneralLedgerEntries --table Features --table SubscriptionPlans --table PlanFeatures --table Subscriptions --table Transactions --table FeatureUsages --table SubscriptionAuditLogs"

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
if exist "%TEMP_ENTITIES_DIR%\Features.cs" (
    ren "%TEMP_ENTITIES_DIR%\Features.cs" "Feature.cs"
    echo   [RENAME] Features.cs -^> Feature.cs
)
if exist "%TEMP_ENTITIES_DIR%\SubscriptionPlans.cs" (
    ren "%TEMP_ENTITIES_DIR%\SubscriptionPlans.cs" "SubscriptionPlan.cs"
    echo   [RENAME] SubscriptionPlans.cs -^> SubscriptionPlan.cs
)
if exist "%TEMP_ENTITIES_DIR%\PlanFeatures.cs" (
    ren "%TEMP_ENTITIES_DIR%\PlanFeatures.cs" "PlanFeature.cs"
    echo   [RENAME] PlanFeatures.cs -^> PlanFeature.cs
)
if exist "%TEMP_ENTITIES_DIR%\Subscriptions.cs" (
    ren "%TEMP_ENTITIES_DIR%\Subscriptions.cs" "Subscription.cs"
    echo   [RENAME] Subscriptions.cs -^> Subscription.cs
)
if exist "%TEMP_ENTITIES_DIR%\Transactions.cs" (
    ren "%TEMP_ENTITIES_DIR%\Transactions.cs" "Transaction.cs"
    echo   [RENAME] Transactions.cs -^> Transaction.cs
)
if exist "%TEMP_ENTITIES_DIR%\FeatureUsages.cs" (
    ren "%TEMP_ENTITIES_DIR%\FeatureUsages.cs" "FeatureUsage.cs"
    echo   [RENAME] FeatureUsages.cs -^> FeatureUsage.cs
)
if exist "%TEMP_ENTITIES_DIR%\SubscriptionAuditLogs.cs" (
    ren "%TEMP_ENTITIES_DIR%\SubscriptionAuditLogs.cs" "SubscriptionAuditLog.cs"
    echo   [RENAME] SubscriptionAuditLogs.cs -^> SubscriptionAuditLog.cs
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
    
    powershell -NoProfile -ExecutionPolicy Bypass -File "%HELPER_PS1%" -Mode Entity -InputFile "!INPUT_FILE!" -OutputFile "!OUTPUT_FILE!"
    if errorlevel 1 (
        echo   [WARNING] Transform failed for !FILENAME!, copying raw file as fallback
        copy /Y "!INPUT_FILE!" "!OUTPUT_FILE!" >nul
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

powershell -Command "$targets = @(); if (Test-Path '%FINAL_ENTITIES_DIR%') { $targets += Get-ChildItem '%FINAL_ENTITIES_DIR%' -Filter '*.cs' -File }; foreach ($file in $targets) { $content = Get-Content $file.FullName -Raw; $content = $content -creplace 'public partial class DeviceTokens', 'public partial class DeviceToken' -creplace 'ICollection<DeviceTokens>', 'ICollection<DeviceToken>' -creplace 'List<DeviceTokens>', 'List<DeviceToken>' -creplace 'virtual DeviceTokens', 'virtual DeviceToken' -creplace '\bProfile\?\s+Profiles\b', 'Profile? Profile' -creplace '\bProfile\s+Profiles\b', 'Profile Profile'; $content = $content.Replace('Profile? Profiles', 'Profile? Profile').Replace('Profile Profiles', 'Profile Profile'); Set-Content $file.FullName -Value $content -NoNewline }"

if errorlevel 1 (
    echo   [WARNING] Failed to patch DeviceToken names in entities.
) else (
    echo   [OK] DeviceToken names patched in entities
)

powershell -Command "$file = '%FINAL_ENTITIES_DIR%\BusinessLocation.cs'; if (Test-Path $file) { $content = Get-Content $file -Raw; if ($content -notmatch 'ICollection<AccountingPeriod>\s+AccountingPeriods') { $insert = '    public virtual ICollection<AccountingPeriod> AccountingPeriods { get; set; } = new List<AccountingPeriod>();'; $content = $content -replace '(public string\? TaxCode \{ get; set; \}\r?\n\r?\n)', ('$1' + $insert); Set-Content $file -Value $content -NoNewline } }"

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
    powershell -NoProfile -ExecutionPolicy Bypass -File "%HELPER_PS1%" -Mode DbContext -InputFile "%DBCONTEXT_INPUT%" -OutputFile "%DBCONTEXT_OUTPUT%"
    if errorlevel 1 (
        echo   [ERROR] Failed to transform DbContext
        pause
        exit /b 1
    )
    
    echo   [OK] Fixed: BizFlowDbContext.cs
) else (
    echo   [WARNING] DbContext not found in TempData
)

powershell -Command "if (Test-Path '%DBCONTEXT_OUTPUT%') { $content = Get-Content '%DBCONTEXT_OUTPUT%' -Raw; $content = $content -creplace 'DbSet<DeviceTokens>', 'DbSet<DeviceToken>' -creplace 'modelBuilder\.Entity<DeviceTokens>', 'modelBuilder.Entity<DeviceToken>' -creplace '\bProfile\?\s+Profiles\b', 'Profile? Profile' -creplace '\bProfile\s+Profiles\b', 'Profile Profile' -creplace 'WithOne\(p => p\.Profiles\)', 'WithOne(p => p.Profile)'; $content = $content.Replace('Profile? Profiles', 'Profile? Profile').Replace('Profile Profiles', 'Profile Profile').Replace('WithOne(p => p.Profiles)', 'WithOne(p => p.Profile)'); Set-Content '%DBCONTEXT_OUTPUT%' -Value $content -NoNewline }"

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
