# BizFlow - Scope of Testing

## 1. Scope of Testing

### Table 5.1: Scope of Testing

| Feature | Function | Role | Description |
|---|---|---|---|
| **Authentication** | Google Login | Guest | Login or register via Google OAuth. Returns tokens and account info. The system creates a new account if the Google account doesn't exist. |
| | Email Login | All Roles | Verify all users can log in with valid email credentials and password. |
| | Phone Login | All Roles | Verify all users can log in with valid phone credentials and password. |
| | Phone Registration | Guest | Register using phone number + password after Firebase Phone Auth OTP verification. Ensure the system validates OTP correctly and creates a new account. |
| | Link Phone | Owner, Employee | Link phone credential to an existing account after Firebase Phone Auth OTP verification. Validate duplicate phone prevention. |
| | Set Password | Owner, Employee | Set password for Google-only accounts that need a fallback credential. Also links an email credential using the Google email. |
| | Change Password | Owner, Employee, consultant | Change password for an account that already has a password. Revokes all refresh tokens on success. |
| | Forgot Password (Send OTP) | Owner, Employee | Send OTP code to the registered email for password reset flow. Validate rate-limiting and OTP expiry. |
| | Forgot Password (Verify OTP) | Guest | Consume email OTP and return a password-reset access JWT (no refresh token). Validate OTP correctness and expiration. |
| | Reset Password | Guest | Set a new password after forgot-password OTP verification. Revokes all refresh tokens. Caller must use password-reset JWT. |
| | Refresh Token | All Roles | Refresh access token using a valid refresh token. Validate token rotation and expiry. |
| | Revoke Token (Logout) | All Roles | Revoke a specific refresh token for single device logout. |
| | Revoke All Tokens | All Roles | Revoke all refresh tokens for an account (logout from all devices). |
| | Delete Account | Owner, Employee | Soft-delete the signed-in account after confirming password. Physical removal and anonymization run later via Hangfire background job. |
| | Get Credentials | Owner, Employee | Get linked credentials (Google, Email, Phone) for an account. |
| | Firebase Custom Token | Owner, Employee | Create Firebase custom token for a profile ID to access Firestore. |
| | Create Consultant | Admin | Admin-only: create consultant account with email + random password, sends welcome email. |
| **Profile Management** | Get Profile | Owner, Employee | Get the signed-in user's profile from storage including name, avatar, tax code. |
| | Update Profile Info | Owner, Employee | Update the signed-in user's profile (full name, tax code). Validate input constraints. |
| | Update Avatar | Owner, Employee | Upload or remove user avatar image via Cloudinary. Validate image format and size. |
| **Business Location Management** | Get Owned Locations | Owner | Retrieve all business locations owned by the user. |
| | Get Work Locations | Employee | Retrieve all business locations where the user is assigned as an employee. |
| | Get Location Detail | Owner, Employee | Get detailed information for a specific business location. |
| | Create Location | Owner | Create a new business location with name, address, and business type. Validate subscription feature access. |
| | Update Location | Owner | Update business location information (name, address, etc.). |
| | Update Location Status | Owner | Enable or disable a business location. Employees cannot access disabled locations. |
| | Delete Location | Owner | Delete a business location. Validate cascading effects on related data. |
| | Get Employees By Location | Owner | Get all employees assigned to a specific business location. |
| | Add Employees To Location | Owner | Assign hired employees to a specific business location. |
| | Remove Employee From Location | Owner | Remove an employee assignment from a specific business location. |
| **Employee Management** | Search User By Contact | Owner | Search for users by phone number or email to invite as employees. |
| | Invite Employee | Owner | Send employment invitation to a user. Validate duplicate invitation prevention. |
| | Remove Employee | Owner | Remove an employee from the hire relationship. |
| | Get Pending Invitations | Employee | Get all pending employment invitations for the current user. |
| | Accept Invitation | Employee | Accept an employment invitation. Triggers notification to owner. |
| | Reject Invitation | Employee | Reject an employment invitation. Triggers notification to owner. |
| **Product Management** | Search Products | Owner, Employee | Search and filter products with pagination. Support keyword search, category filter, status filter. |
| | Quick Search Products | Owner, Employee | Lightweight product search for order flow by business location and optional keyword. |
| | Get Product Detail | Owner, Employee | Get detailed product information by ID including images, price tiers, and stock. |
| | Get Product Sale Items | Owner, Employee | Get product sale items (price tiers) for order creation. |
| | Create Product | Owner, Employee | Create a new product with name, unit, category, images, cost price, and sale items. Validate subscription feature limits. |
| | Update Product | Owner, Employee | Update an existing product's information, images, and sale items. |
| | Update Product Status | Owner, Employee | Change product status (Active/Inactive). |
| | Adjust Product Stock | Owner | Manually adjust product stock to a target quantity. Increase creates an import + stock movement, decrease creates stock movement only. |
| | Bulk Adjust Selling Price | Owner | Bulk adjust selling price by fixed delta on selected sale items. |
| | Delete Product | Owner | Delete product (soft/hard delete based on order history). |
| | Get Cost Price History | Owner | Get cost price history for a product over time. |
| **Import Management** | Create Import | Owner | Create a new import record with status DRAFT. Include supplier info and import items. |
| | Update Import | Owner | Update a DRAFT import's fields and items. Cannot update confirmed imports. |
| | Confirm/Cancel Import | Owner | Confirm (adds quantity to product stock) or cancel an import. Validate stock updates. |
| | List Imports | Owner | Get paginated list of imports with filters (status, date range, location). |
| | Get Import Detail | Owner | Get full import detail including all items and supplier info. |
| | Delete Import | Owner | Delete import. DRAFT → hard-delete. CONFIRMED → subtract stock per item, then hard-delete. |
| **Order Management** | Create Order | Owner, Employee | Create a new sale order with order details, customer info, payment method, and debtor assignment. |
| | Update Order | Owner, Employee | Update an existing order's details before completion. |
| | Complete Order | Owner, Employee | Complete an order: finalize payment, generate revenue, update stock, and record in general ledger. |
| | Cancel Order | Owner, Employee | Cancel an order. Reverse stock and revenue if already completed. |
| | Edit Completed Order | Owner | Edit a completed order: creates a reversal of old order and a new corrected order. |
| | Get Order Detail | Owner, Employee | Get detailed order information including items, payment, and customer info. |
| | List Orders | Owner, Employee | Get paginated list of orders with filters (status, date range, search). |
| **Cost Management** | Create Manual Cost | Owner | Create a manual cost record with amount, type, payment method, and optional image. Records in general ledger. |
| | Update Manual Cost | Owner | Update a manual cost record's information. Updates general ledger accordingly. |
| | List Costs | Owner | Get paginated list of costs with filters (type, date range, location). |
| | Delete Manual Cost | Owner | Delete a manual cost record. Reverses general ledger entries. |
| **Revenue Management** | Create Manual Revenue | Owner | Create a manual revenue record with amount, type, and payment method. Records in general ledger. |
| | Update Manual Revenue | Owner | Update a manual revenue record's information. Updates general ledger accordingly. |
| | List Revenues | Owner | Get paginated list of revenues with filters (type, date range, location). |
| | Delete Manual Revenue | Owner | Delete a manual revenue record. Reverses general ledger entries. |
| **General Ledger** | List Ledger Entries | Owner | Get paginated list of general ledger entries with filters (reference type, transaction type, date range). |
| **Debtor Management** | List Debtors | Owner, Employee | Get paginated list of debtors with summary information (total debt, payment status). |
| | Get Debtor Detail | Owner | Get detailed debtor information including payment history. |
| | Create Debtor | Owner | Create a new debtor record with name, contact info, and initial balance. |
| | Update Debtor | Owner | Update debtor information (name, phone, address). |
| | Update Debtor Status | Owner | Enable or disable a debtor record. |
| | Delete Debtor | Owner | Delete a debtor record. Force delete option for debtors with history. |
| | Get Active Debtors By Location | Owner, Employee | Get active debtors filtered by business location for order assignment. |
| | Record Payment | Owner | Record a debt payment transaction. Updates debtor balance and general ledger. |
| | Get Payments | Owner | Get payment transaction history for a specific debtor. |
| **Accounting Period** | Create Period | Owner | Create a new accounting period (quarterly/yearly) for a business location. |
| | Create Custom Period | Owner | Create a custom accounting period with specific date range. |
| | Get Opening Balance Suggestion | Owner | Get suggested opening balances based on previous period's closing balances. |
| | Get Periods | Owner | Get all accounting periods for a business location. |
| | Get Period Detail | Owner | Get detailed information for a specific accounting period. |
| | Finalize Period | Owner | Finalize an accounting period, locking it from further edits. |
| | Reopen Period | Owner | Reopen a finalized period with reason for audit trail. |
| | Delete Period | Owner | Delete an accounting period and all associated books. |
| | Get Audit Logs | Owner | Get audit log history for a specific accounting period. |
| **Accounting Book** | Create Books | Owner | Create accounting books for a period with template selection and tax method. |
| | List Books | Owner | Get all books for a specific accounting period. |
| | Delete Book | Owner | Delete an accounting book. |
| | Get Book Summary | Owner | Get book summary with formula KPI values (total revenue, cost, tax). |
| | Get Book Rows | Owner | Get data rows for a book with cursor-based pagination. |
| | Get Book Sections | Owner | Get book sections structure with formula values for report rendering. |
| **Subscription Management** | Get Active Plans | All Roles | Get list of active subscription plans available for purchase. |
| | Get Current Subscription | All Roles | Get the current subscription status and feature usage. |
| | Create Checkout Session | Owner | Create a Stripe checkout session for subscription purchase. |
| | Get Transactions | Owner | Get payment transaction history for subscriptions. |
| | Check Feature Access | Owner, Employee | Check if a user has access to a specific feature based on subscription plan. |
| | Revoke Access Grant | Owner | Revoke subscription access grant for an employee. |
| | Get Payment Redirect URL | Owner | Get the redirect URL after Stripe payment (success/cancel). |
| **Subscription Plan (Admin)** | Search Plans | Admin | Get paginated list of all subscription plans with filters. |
| | Get Plan By ID | Admin | Get detailed subscription plan information including Stripe IDs. |
| | Create Plan | Admin | Create a new subscription plan with features, pricing, and Stripe product. |
| | Update Plan | Admin | Update subscription plan details, features, and pricing. |
| | Delete Plan | Admin | Delete a subscription plan. |
| | Set Plan Status | Admin | Activate or deactivate a subscription plan. |
| | Get All Features | Admin | Get list of all available features for plan configuration. |
| **Admin Accounting** | Get Overview | Admin | Get accounting system overview with template and formula statistics. |
| | Template Version Management | Admin | Create, clone, update, activate, deactivate, and delete template versions for accounting books. |
| | Formula Management | Admin | Create, update, clone, and trace formulas for accounting calculations. |
| | Tax Ruleset Management | Admin | Activate/deactivate tax rulesets for accounting calculations. |
| | Row Definition Management | Admin | Create, update, and delete row definitions for template structures. |
| | Field Mapping Management | Admin | Create, update, and delete field mappings for template data binding. |
| | Business Type & Tax Rate | Admin | Update business types and upsert industry tax rates per ruleset. |
| | Preview & Compare | Admin | Preview accounting book output and compare different template versions. |
| | Create template | Admin | Create template for accounting book. |
| **Admin User Management** | Get Users | Admin | Get paginated list of all managed users with filters (role, status, search). |
| | Revoke User Tokens | Admin | Force logout a user by revoking all their refresh tokens. |
| **Notification Management** | Register Device Token | Owner, Employee | Register FCM device token for push notifications. |
| | Unregister Device Token | Owner, Employee | Unregister device token when logging out. |
| | Send Push Notification | System | Send push notifications to user's devices (employee invite, subscription expiry, etc.). |
| | Get User's Notifications | Owner, Employee | Get paginated list of user's notifications. |
| | Get Notification Detail | Owner, Employee | Get detailed notification information. |
| | Get Unread Count | Owner, Employee | Get count of unread notifications. |
| | Mark As Read | Owner, Employee | Mark a specific notification as read. |
| | Mark All As Read | Owner, Employee | Mark all notifications as read. |
| | Template Management | Admin | CRUD operations on notification templates with event codes. |
| | Dispatch Management | Admin | Create and process notification dispatches (immediate/scheduled). |
| **AI Services** | Parse Draft Order (Voice) | Owner, Employee | Use AI to parse voice audio into a draft order with product matching. |
| | Parse Draft Revenue (Voice) | Owner, Employee | Use AI to parse voice audio into a draft revenue record. |
| | Parse Draft Cost (Voice) | Owner, Employee | Use AI to parse voice audio into a draft cost record. |
| | OCR Purchase Invoice | Owner, Employee | Use AI to extract data from purchase invoice images. |
| | OCR Sale Invoice | Owner, Employee | Use AI to extract data from sale invoice images. |
| | Anomaly Detection | System | Check records for anomalies using AI (fire-and-forget via Hangfire). |
| | Revenue Forecast | System | AI batch job for revenue forecasting per location. |
| | Reorder Suggestions | System | AI batch job for inventory reorder suggestions. |
| | Product Insights | System | AI batch job for product performance insights. |
| **Dashboard** | Get Summary | Owner, Employee | Get dashboard summary with revenue, cost, order, and product KPIs for a date range and location. |

---

## 2. Method Descriptions

### Table 5.2: Method Descriptions

| No | Service | Method | Description |
|---|---|---|---|
| **Authentication Service (IAuthService)** | | | |
| 01 | AuthService | `GoogleLoginAsync(idToken, deviceInfo)` | Login or Register via Google OAuth. Verifies the Google ID token, creates account if not exists, issues access + refresh tokens. Output: AuthResponse with tokens and account info. |
| 02 | AuthService | `LoginWithEmailAsync(email, password, deviceInfo)` | Login using email credential and account password. Validates credentials against stored hash. Output: AuthResponse with tokens. |
| 03 | AuthService | `LoginWithPhoneAsync(phone, password, deviceInfo)` | Login using phone credential and account password. Output: AuthResponse with tokens. |
| 04 | AuthService | `RegisterWithPhoneAsync(phone, password, firebaseIdToken, fullName, deviceInfo)` | Register new account using phone + password after Firebase Phone Auth OTP. Output: AuthResponse with tokens. |
| 05 | AuthService | `LinkPhoneAsync(accountId, phone, firebaseIdToken, password)` | Link phone credential to existing account. Output: List of updated credentials. |
| 06 | AuthService | `SetPasswordAsync(accountId, password)` | Set password for Google-only accounts. Also links email credential. Output: void. |
| 07 | AuthService | `ChangePasswordAsync(accountId, currentPassword, newPassword)` | Change password and revoke all refresh tokens. Output: void. |
| 08 | AuthService | `GetProfileAsync(profileId)` | Get user profile from storage. Output: UserProfileDto. |
| 09 | AuthService | `UpdateProfileInfoAsync(profileId, request)` | Update user profile (name, tax code). Output: Updated UserProfileDto. |
| 10 | AuthService | `UpdateAvatarAsync(profileId, request)` | Upload/remove user avatar. Output: Updated UserProfileDto. |
| 11 | AuthService | `RefreshTokenAsync(refreshToken, deviceInfo)` | Refresh access token using valid refresh token. Output: AuthResponse with new tokens. |
| 12 | AuthService | `RevokeRefreshTokenAsync(refreshToken)` | Revoke a specific refresh token (single logout). Output: void. |
| 13 | AuthService | `RevokeAllRefreshTokensAsync(accountId)` | Revoke all refresh tokens (logout all devices). Output: void. |
| 14 | AuthService | `DeleteAccountAsync(accountId, password)` | Soft-delete account after password confirmation. Output: void. |
| 15 | AuthService | `GetCredentialsAsync(accountId)` | Get linked credentials for account. Output: List of CredentialInfo. |
| 16 | AuthService | `CreateFirebaseCustomTokenAsync(profileId)` | Create Firebase custom token for Firestore access. Output: FirebaseCustomTokenResponse. |
| 17 | AuthService | `VerifyEmailOtpForPasswordResetAsync(email, otpCode)` | Consume email OTP for password reset, return reset JWT. Output: VerifyOtpResponse. |
| 18 | AuthService | `ResetPasswordAfterForgotOtpAsync(accountId, newPassword, nonce)` | Set new password after OTP verification. Output: void. |
| 19 | AuthService | `CreateConsultantByAdminAsync(email, fullName)` | Admin creates consultant account. Output: CreateConsultantResponse. |
| **OTP Service (IOtpService)** | | | |
| 20 | OtpService | `SendOtpAsync(request)` | Send OTP code to email for forgot-password flow. Output: SendOtpResponse. |
| 21 | OtpService | `VerifyEmailOtpForPasswordResetAsync(email, otpCode)` | Verify forgot-password OTP and set PasswordResetNonce. Output: PasswordResetOtpVerifiedResult. |
| **Business Location Service (IBusinessLocationService)** | | | |
| 22 | BusinessLocationService | `GetOwnedLocationsAsync(userId)` | Get all business locations owned by user. Output: List of BusinessLocationDto. |
| 23 | BusinessLocationService | `GetWorkLocationsAsync(userId)` | Get all locations where user is an employee. Output: List of BusinessLocationDto. |
| 24 | BusinessLocationService | `GetLocationDetailAsync(userId, locationId)` | Get detailed location information. Output: BusinessLocationDetailDto. |
| 25 | BusinessLocationService | `CreateLocationAsync(userId, request)` | Create new business location. Output: BusinessLocationDto. |
| 26 | BusinessLocationService | `UpdateLocationAsync(userId, locationId, request)` | Update location information. Output: void. |
| 27 | BusinessLocationService | `UpdateLocationStatusAsync(userId, locationId, isActive)` | Enable/disable a location. Output: void. |
| 28 | BusinessLocationService | `DeleteLocationAsync(userId, locationId)` | Delete a business location. Output: void. |
| 29 | BusinessLocationService | `GetEmployeesByLocationAsync(userId, locationId)` | Get employees of a location. Output: EmployeeSummaryListDto. |
| 30 | BusinessLocationService | `AddEmployeesToLocationAsync(ownerId, locationId, employeeIds)` | Assign employees to location. Output: void. |
| 31 | BusinessLocationService | `RemoveEmployeeFromLocationAsync(ownerId, locationId, employeeId)` | Remove employee from location. Output: void. |
| 32 | BusinessLocationService | `ValidateLocationAccessAsync(userId, locationId)` | Guard: validate user access to location. Output: void (throws on failure). |
| 33 | BusinessLocationService | `ValidateOwnerAsync(userId, locationId)` | Guard: validate user is location owner. Output: void (throws ForbiddenException). |
| **Employee Service (IEmployeeService)** | | | |
| 34 | EmployeeService | `SearchUserByContactAsync(ownerId, query)` | Search users by phone/email for invitation. Output: List of UserSearchResultDto. |
| 35 | EmployeeService | `InviteEmployeeAsync(ownerId, employeeId)` | Send employment invitation. Output: HireDto. |
| 36 | EmployeeService | `RemoveEmployeeAsync(ownerId, employeeId)` | Remove an employee. Output: void. |
| 37 | EmployeeService | `GetPendingInvitationsAsync(employeeId)` | Get pending invitations. Output: List of EmployeeInvitationDto. |
| 38 | EmployeeService | `AcceptInvitationAsync(employeeId, hireId)` | Accept invitation. Output: void. |
| 39 | EmployeeService | `RejectInvitationAsync(employeeId, hireId)` | Reject invitation. Output: void. |
| **Product Service (IProductService)** | | | |
| 40 | ProductService | `SearchProductsAsync(userId, query)` | Search/filter products with pagination. Output: PaginatedResponse of ProductSummaryDto. |
| 41 | ProductService | `SearchQuickProductsAsync(userId, locationId, search)` | Lightweight product search for order flow. Output: List of ProductQuickSearchDto. |
| 42 | ProductService | `GetProductDetailAsync(userId, productId)` | Get detailed product info. Output: ProductDetailDto. |
| 43 | ProductService | `GetProductSaleItemsAsync(userId, productId)` | Get product price tiers. Output: ProductSaleItemsResponseDto. |
| 44 | ProductService | `CreateProductAsync(userId, request)` | Create new product. Output: Tuple of ProductSummaryDto and Warnings. |
| 45 | ProductService | `UpdateProductAsync(userId, productId, request)` | Update existing product. Output: Tuple of ProductSummaryDto and Warnings. |
| 46 | ProductService | `UpdateProductStatusAsync(userId, productId, status)` | Update product status. Output: void. |
| 47 | ProductService | `AdjustProductStockAsync(userId, productId, request)` | Manually adjust stock quantity. Output: ProductSummaryDto. |
| 48 | ProductService | `BulkAdjustSellingPriceAsync(userId, request)` | Bulk adjust selling prices. Output: void. |
| 49 | ProductService | `DeleteProductAsync(userId, productId)` | Delete product (soft/hard). Output: void. |
| 50 | ProductService | `GetCostPriceHistoryAsync(userId, productId)` | Get cost price history. Output: CostPriceHistoryDto. |
| **Import Service (IImportService)** | | | |
| 51 | ImportService | `GetTemplateAsync()` | Get active import JSON schema. Output: ImportSchemaDto. |
| 52 | ImportService | `CreateImportAsync(userId, request)` | Create import (DRAFT). Output: ImportSummaryDto. |
| 53 | ImportService | `UpdateImportAsync(userId, importId, request)` | Update DRAFT import. Output: ImportSummaryDto. |
| 54 | ImportService | `PatchImportAsync(userId, importId, request)` | Confirm or cancel import. Output: ImportPatchResultDto. |
| 55 | ImportService | `ListImportsAsync(userId, query)` | List imports with pagination. Output: PaginatedResponse of ImportSummaryDto. |
| 56 | ImportService | `GetImportDetailAsync(userId, importId)` | Get full import detail. Output: ImportDetailDto. |
| 57 | ImportService | `CreateInventoryAdjustmentImportAsync(locationId, productId, quantity, costPrice, memo)` | Create confirmed inventory-adjustment import. Output: ImportId (long). |
| 58 | ImportService | `DeleteImportAsync(userId, importId)` | Delete import with stock reversal if confirmed. Output: void. |
| **Order Service (IOrderService)** | | | |
| 59 | OrderService | `CreateAsync(userId, request)` | Create new sale order. Output: OrderActionResultDto. |
| 60 | OrderService | `UpdateAsync(userId, orderId, request)` | Update existing order. Output: OrderActionResultDto. |
| 61 | OrderService | `CompleteAsync(userId, orderId, request)` | Complete order (finalize payment, stock, revenue). Output: OrderActionResultDto. |
| 62 | OrderService | `CancelAsync(userId, orderId, request)` | Cancel order with reversal. Output: OrderDto. |
| 63 | OrderService | `EditCompletedSaveAsync(userId, oldOrderId, request)` | Edit completed order (reversal + new). Output: EditCompletedSaveResultDto. |
| 64 | OrderService | `GetDetailAsync(userId, orderId)` | Get order detail. Output: OrderDto. |
| 65 | OrderService | `ListAsync(userId, query)` | List orders with pagination. Output: PaginatedResponse of OrderDto. |
| **Cost Service (ICostService)** | | | |
| 66 | CostService | `CreateManualAsync(userId, request)` | Create manual cost and record in ledger. Output: CostDto. |
| 67 | CostService | `UpdateManualAsync(userId, costId, request)` | Update manual cost. Output: CostDto. |
| 68 | CostService | `ListAsync(userId, query)` | List costs with pagination. Output: PaginatedResponse of CostDto. |
| 69 | CostService | `DeleteManualAsync(userId, costId)` | Delete manual cost and reverse ledger. Output: void. |
| 70 | CostService | `CreateImportCostAsync(userId, import, documentNumber, documentDate)` | Create cost from import confirmation. Output: Cost entity. |
| 71 | CostService | `ReverseImportCostAsync(userId, import, reason)` | Reverse import cost on deletion. Output: void. |
| **Revenue Service (IRevenueService)** | | | |
| 72 | RevenueService | `CreateManualAsync(userId, request)` | Create manual revenue record. Output: RevenueDto. |
| 73 | RevenueService | `UpdateManualAsync(userId, revenueId, request)` | Update manual revenue. Output: RevenueDto. |
| 74 | RevenueService | `ListAsync(userId, query)` | List revenues with pagination. Output: PaginatedResponse of RevenueDto. |
| 75 | RevenueService | `DeleteManualAsync(userId, revenueId)` | Delete manual revenue and reverse ledger. Output: void. |
| **General Ledger Service (IGeneralLedgerService)** | | | |
| 76 | GeneralLedgerService | `ListAsync(userId, query)` | List ledger entries with pagination. Output: PaginatedResponse of GeneralLedgerEntryDto. |
| 77 | GeneralLedgerService | `RecordImportCostAsync(cost)` | Record import cost in ledger. Output: GeneralLedgerEntry. |
| 78 | GeneralLedgerService | `RecordManualCostAsync(cost)` | Record manual cost in ledger. Output: GeneralLedgerEntry. |
| 79 | GeneralLedgerService | `ReverseCostEntriesAsync(cost, reason)` | Reverse cost entries in ledger. Output: Number of reversed entries. |
| 80 | GeneralLedgerService | `RecordDebtPaymentAsync(transaction, locationId)` | Record debt payment in ledger. Output: GeneralLedgerEntry. |
| 81 | GeneralLedgerService | `RecordSaleRevenueAsync(revenue)` | Record sale revenue in ledger. Output: GeneralLedgerEntry. |
| 82 | GeneralLedgerService | `RecordManualRevenueAsync(revenue)` | Record manual revenue in ledger. Output: GeneralLedgerEntry. |
| 83 | GeneralLedgerService | `ReverseRevenueEntriesAsync(revenue, reason)` | Reverse revenue entries. Output: Number of reversed entries. |
| **Debtor Service (IDebtorService)** | | | |
| 84 | DebtorService | `ListAsync(userId, query)` | List debtors with pagination. Output: PaginatedResponse of DebtorSummaryDto. |
| 85 | DebtorService | `GetDetailAsync(userId, debtorId)` | Get debtor detail. Output: DebtorDetailDto. |
| 86 | DebtorService | `CreateAsync(userId, request)` | Create debtor. Output: DebtorDetailDto. |
| 87 | DebtorService | `UpdateAsync(userId, debtorId, request)` | Update debtor. Output: DebtorDetailDto. |
| 88 | DebtorService | `UpdateStatusAsync(userId, debtorId, isActive)` | Enable/disable debtor. Output: DebtorDetailDto. |
| 89 | DebtorService | `DeleteAsync(userId, debtorId, forceDelete)` | Delete debtor. Output: void. |
| 90 | DebtorService | `GetActiveDebtorsByLocationAsync(userId, locationId)` | Get active debtors by location. Output: List of DebtorMinimalDto. |
| 91 | DebtorService | `RecordPaymentAsync(userId, debtorId, request)` | Record debt payment. Output: DebtorPaymentDto. |
| 92 | DebtorService | `GetPaymentsAsync(userId, debtorId)` | Get payment history. Output: List of DebtorPaymentDto. |
| **Accounting Period Service (IAccountingPeriodService)** | | | |
| 93 | AccountingPeriodService | `CreatePeriodAsync(locationId, userId, request)` | Create accounting period. Output: AccountingPeriodDto. |
| 94 | AccountingPeriodService | `CreateCustomPeriodAsync(locationId, userId, request)` | Create custom period. Output: AccountingPeriodDto. |
| 95 | AccountingPeriodService | `GetOpeningBalanceSuggestionAsync(locationId, userId, request)` | Get opening balance suggestion. Output: OpeningBalanceSuggestionDto. |
| 96 | AccountingPeriodService | `GetPeriodsAsync(locationId, userId)` | Get all periods. Output: List of AccountingPeriodDto. |
| 97 | AccountingPeriodService | `GetPeriodDetailAsync(locationId, periodId, userId)` | Get period detail. Output: AccountingPeriodDto. |
| 98 | AccountingPeriodService | `FinalizePeriodAsync(locationId, periodId, userId)` | Finalize period. Output: AccountingPeriodDto. |
| 99 | AccountingPeriodService | `ReopenPeriodAsync(locationId, periodId, userId, reason)` | Reopen period with reason. Output: AccountingPeriodDto. |
| 100 | AccountingPeriodService | `DeletePeriodAsync(locationId, periodId, userId)` | Delete period. Output: void. |
| 101 | AccountingPeriodService | `GetAuditLogsAsync(locationId, periodId, userId)` | Get period audit logs. Output: List of AccountingPeriodAuditLogDto. |
| **Accounting Book Service (IAccountingBookService)** | | | |
| 102 | AccountingBookService | `CreateBooksAsync(locationId, userId, request)` | Create accounting books. Output: CreateBooksResponse. |
| 103 | AccountingBookService | `ListBooksAsync(locationId, userId, periodId)` | List books for period. Output: List of BookListItemDto. |
| 104 | AccountingBookService | `DeleteBookAsync(locationId, userId, bookId)` | Delete book. Output: void. |
| 105 | AccountingBookService | `GetBookSummaryAsync(locationId, userId, bookId)` | Get book summary with KPIs. Output: BookSummaryResponse. |
| 106 | AccountingBookService | `GetBookRowsAsync(locationId, userId, bookId, cursor, batchSize)` | Get book rows (paginated). Output: BookRowsResponse. |
| 107 | AccountingBookService | `GetBookSectionsAsync(locationId, userId, bookId)` | Get book sections. Output: BookSectionsResponse. |
| **Book Rendering Service (IBookRenderingService)** | | | |
| 108 | BookRenderingService | `RenderRowsAsync(context, cursor, batchSize)` | Render data rows for a book with cursor pagination. Output: BookRenderResult. |
| 109 | BookRenderingService | `ComputeSummaryAsync(context)` | Compute formula summary values (KPIs). Output: BookFormulaSummary. |
| 110 | BookRenderingService | `RenderSectionsAsync(context)` | Render book sections with formula values. Output: BookSectionsRenderResult. |
| **Formula Engine (IFormulaEngine)** | | | |
| 111 | FormulaEngine | `EvaluateFormulasAsync(context, formulas)` | Evaluate formulas for a book. Output: Dictionary of Code → Value. |
| 112 | FormulaEngine | `EvaluateFormulasWithBreakdownAsync(context, formulas)` | Evaluate with per-group breakdowns. Output: FormulaEvaluationResults. |
| 113 | FormulaEngine | `TraceFormulaAsync(context, formula)` | Trace single formula execution. Output: FormulaTraceResult. |
| **Subscription Service (ISubscriptionService)** | | | |
| 114 | SubscriptionService | `GetCurrentSubscriptionAsync(profileId)` | Get current subscription status. Output: CurrentSubscriptionDto. |
| 115 | SubscriptionService | `CreateCheckoutSessionAsync(profileId, planId, platform, quantity)` | Create Stripe checkout session. Output: CheckoutSessionResponseDto. |
| 116 | SubscriptionService | `CheckFeatureAccessAsync(profileId, locationId, featureCode, incrementUsage)` | Check feature access by subscription. Output: bool. |
| 117 | SubscriptionService | `EvaluateFeatureAccessAsync(profileId, locationId, featureCode)` | Evaluate feature access with detail. Output: FeatureAccessEvaluationResult. |
| 118 | SubscriptionService | `GetTransactionsAsync(profileId, page, pageSize)` | Get payment transactions. Output: List of TransactionDto. |
| 119 | SubscriptionService | `HandleCheckoutCompletedAsync(session)` | Handle Stripe checkout completed webhook. Output: void. |
| 120 | SubscriptionService | `HandleChargeRefundedAsync(paymentIntentId)` | Handle Stripe refund webhook. Output: void. |
| **Notification Service (INotificationService)** | | | |
| 121 | NotificationService | `RegisterDeviceTokenAsync(userId, token, deviceName, platform)` | Register FCM device token. Output: void. |
| 122 | NotificationService | `UnregisterDeviceTokenAsync(userId, token)` | Unregister device token. Output: void. |
| 123 | NotificationService | `SendToAllDevicesAsync(userId, title, body)` | Send push to all user devices. Output: void. |
| 124 | NotificationService | `GetUserNotificationsAsync(userId, query)` | Get user notifications paginated. Output: PaginatedResponse of UserNotificationDto. |
| 125 | NotificationService | `GetUnreadCountAsync(userId)` | Get unread count. Output: int. |
| 126 | NotificationService | `MarkAsReadAsync(userId, userNotificationId)` | Mark notification as read. Output: bool. |
| 127 | NotificationService | `MarkAllAsReadAsync(userId)` | Mark all as read. Output: int (count). |
| 128 | NotificationService | `CreateDispatchAsync(createdByUserId, request)` | Create notification dispatch. Output: NotificationDispatchDto. |
| 129 | NotificationService | `ProcessDispatchAsync(dispatchId)` | Process a notification dispatch. Output: void. |
| 130 | NotificationService | `ArchiveExpiredNotificationsAsync()` | Archive expired notifications. Output: void. |
| **AI Service Client (IAiServiceClient)** | | | |
| 131 | AiServiceClient | `ParseDraftOrderAsync(audioStream, mimeType, locationId)` | Parse voice to draft order. Output: AiDraftOrderResultDto. |
| 132 | AiServiceClient | `ParseDraftRevenueAsync(audioStream, mimeType, locationId)` | Parse voice to draft revenue. Output: AiDraftRevenueResultDto. |
| 133 | AiServiceClient | `ParseDraftCostAsync(audioStream, mimeType, locationId)` | Parse voice to draft cost. Output: AiDraftCostResultDto. |
| 134 | AiServiceClient | `OcrPurchaseInvoiceAsync(imageStream, mimeType, locationId)` | OCR purchase invoice. Output: AiPurchaseInvoiceResultDto. |
| 135 | AiServiceClient | `OcrSaleInvoiceAsync(imageStream, mimeType, locationId)` | OCR sale invoice. Output: AiSaleInvoiceResultDto. |
| 136 | AiServiceClient | `CheckAnomalyAsync(locationId, recordType, recordId)` | Check record anomaly. Output: AiCheckRecordResultDto. |
| 137 | AiServiceClient | `TriggerForecastAsync(locationIds)` | Trigger revenue forecast batch. Output: AiBatchJobResultDto. |
| 138 | AiServiceClient | `TriggerReorderAsync(locationIds)` | Trigger reorder suggestion batch. Output: AiBatchJobResultDto. |
| **Dashboard Service (IUserDashboardService)** | | | |
| 139 | UserDashboardService | `GetSummaryAsync(userId, query)` | Get dashboard summary KPIs. Output: DashboardSummaryResponse. |
| **Unit of Work (IUnitOfWork)** | | | |
| 140 | UnitOfWork | `SaveChangesAsync()` | Save all pending changes to the database. Output: Number of affected rows. |
| 141 | UnitOfWork | `BeginTransactionAsync()` | Begin a database transaction. Output: void. |
| 142 | UnitOfWork | `CommitTransactionAsync()` | Commit the current transaction. Output: void. |
| 143 | UnitOfWork | `RollbackTransactionAsync()` | Rollback the current transaction. Output: void. |
| 144 | UnitOfWork | `ExecuteResilientAsync(action)` | Execute action with retry strategy for transient failures. Output: void or TResult. |
| 145 | UnitOfWork | `ExecuteResilientPurgeAsync(action)` | Resilient transaction for bulk SQL without implicit SaveChanges. Output: void. |
