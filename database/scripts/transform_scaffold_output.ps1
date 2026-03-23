param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Entity', 'DbContext')]
    [string]$Mode,

    [Parameter(Mandatory = $true)]
    [string]$InputFile,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

$typeMap = [ordered]@{
    'Roles' = 'Role'
    'Accounts' = 'Account'
    'Profiles' = 'Profile'
    'Credentials' = 'Credential'
    'RefreshTokens' = 'RefreshToken'
    'DeviceTokens' = 'DeviceToken'
    'BusinessTypes' = 'BusinessType'
    'BusinessTypeTaxes' = 'BusinessTypeTax'
    'BusinessLocations' = 'BusinessLocation'
    'UserLocationAssignments' = 'UserLocationAssignment'
    'Products' = 'Product'
    'ProductPricePolicies' = 'ProductPricePolicy'
    'Imports' = 'Import'
    'ProductsImports' = 'ProductImport'
    'ProductImports' = 'ProductImport'
    'SaleItems' = 'SaleItem'
    'Hires' = 'Hire'
    'ImportSchemas' = 'ImportSchema'
    'ImportSchemaVersions' = 'ImportSchemaVersion'
    'StockMovements' = 'StockMovement'
    'Debtors' = 'Debtor'
    'DebtorPaymentTransactions' = 'DebtorPaymentTransaction'
    'Orders' = 'Order'
    'OrderDetails' = 'OrderDetail'
    'Revenues' = 'Revenue'
    'Costs' = 'Cost'
    'GeneralLedgerEntries' = 'GeneralLedgerEntry'
    'Features' = 'Feature'
    'SubscriptionPlans' = 'SubscriptionPlan'
    'PlanFeatures' = 'PlanFeature'
    'Subscriptions' = 'Subscription'
    'Transactions' = 'Transaction'
    'FeatureUsages' = 'FeatureUsage'
    'SubscriptionAuditLogs' = 'SubscriptionAuditLog'
}

function Apply-CommonTypeReplacements {
    param([string]$Content)

    foreach ($old in $typeMap.Keys) {
        $new = $typeMap[$old]
        $oldEscaped = [regex]::Escape($old)
        $newEscaped = [regex]::Escape($new)

        $Content = $Content -creplace "public partial class $old\b", "public partial class $new"
        $Content = $Content -creplace "DbSet<$old>", "DbSet<$new>"
        $Content = $Content -creplace "modelBuilder\.Entity<$old>", "modelBuilder.Entity<$new>"
        $Content = $Content -creplace "ICollection<$old>", "ICollection<$new>"
        $Content = $Content -creplace "List<$old>", "List<$new>"
        $Content = $Content -creplace "virtual $old\b", "virtual $new"
        $Content = $Content -creplace "HasForeignKey<$old>", "HasForeignKey<$new>"

        # Fix one-to-one navigation property names like: Profile? Profiles -> Profile? Profile
        $Content = $Content -creplace "(\\b$newEscaped\\??\\s+)$oldEscaped\\b", "`$1$new"
    }

    # Explicit safety net for common one-to-one scaffold naming from EF
    $Content = $Content.Replace('Profile? Profiles', 'Profile? Profile')
    $Content = $Content.Replace('Profile Profiles', 'Profile Profile')
    $Content = $Content -creplace '\bProfile\?\s+Profiles\b', 'Profile? Profile'
    $Content = $Content -creplace '\bProfile\s+Profiles\b', 'Profile Profile'
    $Content = $Content.Replace('WithOne(p => p.Profiles)', 'WithOne(p => p.Profile)')
    $Content = $Content -creplace 'WithOne\(p => p\.Profiles\)', 'WithOne(p => p.Profile)'
    return $Content
}

$content = Get-Content $InputFile -Raw

if ($Mode -eq 'Entity') {
    $content = $content -creplace 'namespace BizFlow\.Infrastructure\.TempEntities', 'namespace BizFlow.Domain.Entities'
    $content = $content -creplace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', ''
    $content = Apply-CommonTypeReplacements -Content $content
}

if ($Mode -eq 'DbContext') {
    $content = $content -creplace 'namespace BizFlow\.Infrastructure\.TempData', 'namespace BizFlow.Infrastructure.DataContext'
    $content = $content -creplace 'using BizFlow\.Infrastructure\.TempEntities;\r?\n', ''

    if ($content -notmatch 'using BizFlow\.Domain\.Entities;') {
        $content = $content -replace 'using System;\r?\n', "using System;`r`nusing BizFlow.Domain.Entities;`r`n"
    }

    $content = Apply-CommonTypeReplacements -Content $content
}

Set-Content -Path $OutputFile -Value $content -NoNewline
