using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application.Common.Constants
{
    /// <summary>
    /// Message keys for localization
    /// </summary>
    public static class MessageKeys
    {
        // ============ COMMON ============
        public const string Success = "COMMON_SUCCESS";
        public const string Error = "COMMON_ERROR";
        public const string NotFound = "COMMON_NOT_FOUND";
        public const string BadRequest = "COMMON_BAD_REQUEST";
        public const string Unauthorized = "COMMON_UNAUTHORIZED";
        public const string Forbidden = "COMMON_FORBIDDEN";
        public const string InternalServerError = "COMMON_INTERNAL_SERVER_ERROR";
        public const string DatabaseUpdateError = "COMMON_DATABASE_UPDATE_ERROR";
        public const string ValidationError = "COMMON_VALIDATION_ERROR";
        public const string DataRetrievedSuccessfully = "COMMON_DATA_RETRIEVED";
        public const string DataCreatedSuccessfully = "COMMON_DATA_CREATED";
        public const string DataUpdatedSuccessfully = "COMMON_DATA_UPDATED";
        public const string DataDeletedSuccessfully = "COMMON_DATA_DELETED";
        public const string ImageFileTooLarge = "IMAGE_FILE_TOO_LARGE";
        public const string ImageInvalidFileType = "IMAGE_INVALID_FILE_TYPE";
        public const string ImageUploadFailed = "IMAGE_UPLOAD_FAILED";
        public const string ReversalDescriptionFormat = "LEDGER_REVERSAL_DESCRIPTION_FORMAT";
        public const string LedgerDebtPaymentDescription = "LEDGER_DEBT_PAYMENT_DESCRIPTION";
        public const string ManualCostUpdatedReversalReason = "LEDGER_MANUAL_COST_UPDATED_REASON";
        public const string ManualCostDeletedReversalReason = "LEDGER_MANUAL_COST_DELETED_REASON";
        public const string ImportCancelledReversalReason = "LEDGER_IMPORT_CANCELLED_REASON";
        public const string ManualRevenueUpdatedReversalReason = "LEDGER_MANUAL_REVENUE_UPDATED_REASON";
        public const string ManualRevenueDeletedReversalReason = "LEDGER_MANUAL_REVENUE_DELETED_REASON";
        public const string OrderCancelledReversalReason = "LEDGER_ORDER_CANCELLED_REASON";
        public const string OrderReplacedReason = "ORDER_REPLACED_REASON";
        public const string OrderAutoRollbackNote = "ORDER_AUTO_ROLLBACK_NOTE";
        public const string OrderRevenueDescriptionFormat = "ORDER_REVENUE_DESCRIPTION_FORMAT";
        public const string InvalidLookbackUnit = "LEDGER_INVALID_LOOKBACK_UNIT";
        public const string LedgerDateOutOfRange = "LEDGER_DATE_OUT_OF_RANGE";
        public const string LedgerInvalidDateRange = "LEDGER_INVALID_DATE_RANGE";

        // ============ LEDGER VALIDATION ============
        public const string LedgerInvalidViewMode = "LEDGER_INVALID_VIEW_MODE";
        public const string LedgerInvalidTransactionType = "LEDGER_INVALID_TRANSACTION_TYPE";
        public const string LedgerInvalidReferenceType = "LEDGER_INVALID_REFERENCE_TYPE";
        public const string LedgerInvalidMoneyChannel = "LEDGER_INVALID_MONEY_CHANNEL";
        public const string LedgerEffectiveModeRequiresToDate = "LEDGER_EFFECTIVE_MODE_REQUIRES_TODATE";

        // ============ AUTHENTICATION ============
        public const string LoginSuccess = "AUTH_LOGIN_SUCCESS";
        public const string LoginFailed = "AUTH_LOGIN_FAILED";
        public const string LogoutSuccess = "AUTH_LOGOUT_SUCCESS";
        public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
        public const string InvalidToken = "AUTH_INVALID_TOKEN";
        public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
        public const string AccountCreated = "AUTH_ACCOUNT_CREATED";
        public const string PasswordSet = "AUTH_PASSWORD_SET";
        public const string PasswordAlreadySet = "AUTH_PASSWORD_ALREADY_SET";
        public const string PasswordInvalidFormat = "AUTH_PASSWORD_INVALID_FORMAT";
        public const string PasswordChanged = "AUTH_PASSWORD_CHANGED";
        public const string NoPasswordToChange = "AUTH_NO_PASSWORD_TO_CHANGE";
        public const string CurrentPasswordIncorrect = "AUTH_CURRENT_PASSWORD_INCORRECT";
        public const string NewPasswordSameAsCurrent = "AUTH_NEW_PASSWORD_SAME_AS_CURRENT";
        public const string TokenRefreshed = "AUTH_TOKEN_REFRESHED";
        public const string InvalidGoogleToken = "AUTH_INVALID_GOOGLE_TOKEN";
        public const string InvalidFirebaseToken = "AUTH_INVALID_FIREBASE_TOKEN";
        public const string PhoneAlreadyExists = "AUTH_PHONE_ALREADY_EXISTS";
        public const string PhoneAlreadyLinked = "AUTH_PHONE_ALREADY_LINKED";
        public const string PhoneVerificationMismatch = "AUTH_PHONE_VERIFICATION_MISMATCH";
        public const string PhoneRegisterSuccess = "AUTH_PHONE_REGISTER_SUCCESS";
        public const string PhoneLinkSuccess = "AUTH_PHONE_LINK_SUCCESS";
        public const string AccountNotFound = "AUTH_ACCOUNT_NOT_FOUND";
        public const string AccountInactiveOrDeleted = "AUTH_ACCOUNT_INACTIVE_OR_DELETED";
        /// <summary>Cannot delete account because no password is set (OAuth-only sign-in).</summary>
        public const string NoPasswordToDelete = "AUTH_NO_PASSWORD_TO_DELETE";

        public const string IdTokenRequired = "AUTH_ID_TOKEN_REQUIRED";
        public const string EmailRequired = "AUTH_EMAIL_REQUIRED";
        public const string PasswordRequired = "AUTH_PASSWORD_REQUIRED";
        public const string PhoneRequired = "AUTH_PHONE_REQUIRED";
        public const string FirebaseIdTokenRequired = "AUTH_FIREBASE_ID_TOKEN_REQUIRED";
        public const string RefreshTokenRequired = "AUTH_REFRESH_TOKEN_REQUIRED";
        public const string CurrentPasswordRequired = "AUTH_CURRENT_PASSWORD_REQUIRED";
        public const string PasswordRequiredWhenNoPassword = "AUTH_PASSWORD_REQUIRED_WHEN_NO_PASSWORD";
        public const string DefaultRoleNotFound = "AUTH_DEFAULT_ROLE_NOT_FOUND";
        public const string AccountHasNoProfile = "AUTH_ACCOUNT_HAS_NO_PROFILE";
        /// <summary>Forgot-password: no active account with this email credential.</summary>
        public const string ForgotPasswordEmailNotRegistered = "AUTH_FORGOT_PASSWORD_EMAIL_NOT_REGISTERED";
        /// <summary>Forgot-password reset JWT already used or invalid nonce.</summary>
        public const string PasswordResetTokenInvalidOrUsed = "AUTH_PASSWORD_RESET_TOKEN_INVALID_OR_USED";
        public const string RefreshTokenInvalidOrExpired = "AUTH_REFRESH_TOKEN_INVALID_OR_EXPIRED";
        public const string RefreshTokenReuseDetected = "AUTH_REFRESH_TOKEN_REUSE_DETECTED";
        public const string InvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";
        public const string GoogleAuthClientIdsNotConfigured = "AUTH_GOOGLE_AUTH_CLIENT_IDS_NOT_CONFIGURED";
        public const string FirebaseProjectIdNotConfigured = "AUTH_FIREBASE_PROJECT_ID_NOT_CONFIGURED";
        public const string FirebaseAppNotInitialized = "AUTH_FIREBASE_APP_NOT_INITIALIZED";
        public const string FirebaseServiceAccountFileNotFound = "AUTH_FIREBASE_SERVICE_ACCOUNT_FILE_NOT_FOUND";
        public const string FirebaseTokenPayloadInvalid = "AUTH_FIREBASE_TOKEN_PAYLOAD_INVALID";
        public const string FirebaseTokenNoPhoneNumber = "AUTH_FIREBASE_TOKEN_NO_PHONE_NUMBER";
        public const string FirebasePhoneNumberEmpty = "AUTH_FIREBASE_PHONE_NUMBER_EMPTY";
        public const string PhoneInvalidFormat = "AUTH_PHONE_INVALID_FORMAT";

        public const string LogoutAllSuccess = "AUTH_LOGOUT_ALL_SUCCESS";
        public const string AdminRevokeAllRefreshTokensSuccess = "AUTH_ADMIN_REVOKE_ALL_REFRESH_TOKENS_SUCCESS";
        public const string AdminCannotManageAdminAccount = "AUTH_ADMIN_CANNOT_MANAGE_ADMIN_ACCOUNT";
        public const string AdminForgotPasswordNotAllowed = "AUTH_ADMIN_FORGOT_PASSWORD_NOT_ALLOWED";

        public const string ConsultantCannotLinkPhone = "AUTH_CONSULTANT_CANNOT_LINK_PHONE";


        /// <summary>Admin created a consultant account (welcome email sent or attempted).</summary>
        public const string GoogleAlreadyLinked = "AUTH_GOOGLE_ALREADY_LINKED";
        public const string GoogleAlreadyExists = "AUTH_GOOGLE_ALREADY_EXISTS";
        public const string GoogleLinkSuccess = "AUTH_GOOGLE_LINK_SUCCESS";

        public const string ConsultantCreated = "AUTH_CONSULTANT_CREATED";
        public const string ConsultantRoleNotFound = "AUTH_CONSULTANT_ROLE_NOT_FOUND";
        public const string AccountIsNotConsultant = "AUTH_ACCOUNT_IS_NOT_CONSULTANT";
        public const string ConsultantDeleted = "AUTH_CONSULTANT_DELETED";

        /// <summary>Validation errors for profile info (fullName, taxCode, ...).</summary>
        public const string ProfileFullNameRequired = "AUTH_PROFILE_FULLNAME_REQUIRED";
        public const string ProfileFullNameTooLong = "AUTH_PROFILE_FULLNAME_TOO_LONG";
        public const string ProfileFullNameInvalidType = "AUTH_PROFILE_FULLNAME_INVALID_TYPE";
        public const string ProfileTaxCodeTooLong = "AUTH_PROFILE_TAXCODE_TOO_LONG";
        public const string ProfileTaxCodeInvalidType = "AUTH_PROFILE_TAXCODE_INVALID_TYPE";
        public const string ProfileAtLeastOneFieldRequired = "AUTH_PROFILE_AT_LEAST_ONE_FIELD_REQUIRED";

        // ============ ROLE ============
        public const string RoleNotFound = "ROLE_NOT_FOUND";
        public const string RoleAlreadyExists = "ROLE_ALREADY_EXISTS";
        public const string RoleCreatedSuccessfully = "ROLE_CREATED";
        public const string RoleUpdatedSuccessfully = "ROLE_UPDATED";
        public const string RoleDeletedSuccessfully = "ROLE_DELETED";
        public const string RolesRetrievedSuccessfully = "ROLE_RETRIEVED";
        public const string RoleNameRequired = "ROLE_NAME_REQUIRED";

        // ============ USER ============
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string UserAlreadyExists = "USER_ALREADY_EXISTS";
        public const string UserCreatedSuccessfully = "USER_CREATED";
        public const string UserUpdatedSuccessfully = "USER_UPDATED";
        public const string UserDeletedSuccessfully = "USER_DELETED";
        public const string UsersRetrievedSuccessfully = "USER_RETRIEVED";
        public const string EmailAlreadyExists = "USER_EMAIL_EXISTS";
        public const string UsernameAlreadyExists = "USER_USERNAME_EXISTS";
        public const string InvalidEmailFormat = "USER_INVALID_EMAIL";
        public const string PasswordTooWeak = "USER_PASSWORD_WEAK";

        // ============ BUSINESS LOCATION ============
        public const string LocationAlreadyExists = "LOCATION_ALREADY_EXISTS";
        public const string LocationInactive = "LOCATION_INACTIVE";
        public const string LocationHasOrders = "LOCATION_HAS_ORDERS";

        // ============ HIRE ============
        public const string EmployeesNotHired = "HIRE_EMPLOYEES_NOT_HIRED";
        public const string EmployeesAlreadyAssigned = "LOCATION_EMPLOYEES_ALREADY_ASSIGNED";
        public const string EmployeeInviteSuccess = "EMPLOYEE_INVITE_SUCCESS";
        public const string EmployeeAlreadyHired = "EMPLOYEE_ALREADY_HIRED";
        public const string EmployeeHasActiveAssignments = "EMPLOYEE_HAS_ACTIVE_ASSIGNMENTS";
        public const string EmployeeSearchEmpty = "EMPLOYEE_SEARCH_EMPTY";
        public const string DeviceTokenRegistered = "DEVICE_TOKEN_REGISTERED";
        public const string FirebaseConfigMissing = "FIREBASE_CONFIG_MISSING";
        public const string NotificationUnreadCountRetrieved = "NOTIFICATION_UNREAD_COUNT_RETRIEVED";
        public const string NotificationMarkedAsRead = "NOTIFICATION_MARKED_AS_READ";
        public const string NotificationMarkedAllAsRead = "NOTIFICATION_MARKED_ALL_AS_READ";
        public const string NotificationTemplateSaved = "NOTIFICATION_TEMPLATE_SAVED";
        public const string NotificationTemplateUpdated = "NOTIFICATION_TEMPLATE_UPDATED";
        public const string NotificationDispatchCreated = "NOTIFICATION_DISPATCH_CREATED";
        public const string NotificationDispatchCancelled = "NOTIFICATION_DISPATCH_CANCELLED";
        public const string NotificationDispatchProcessed = "NOTIFICATION_DISPATCH_PROCESSED";
        public const string NotificationDispatchCannotCancel = "NOTIFICATION_DISPATCH_CANNOT_CANCEL";
        public const string NotificationRecipientGroupInvalid = "NOTIFICATION_RECIPIENT_GROUP_INVALID";
        public const string NotificationRecipientLocationRequired = "NOTIFICATION_RECIPIENT_LOCATION_REQUIRED";
        public const string NotificationRecipientSelectionConflict = "NOTIFICATION_RECIPIENT_SELECTION_CONFLICT";
        public const string NotificationRecipientGroupEmpty = "NOTIFICATION_RECIPIENT_GROUP_EMPTY";
        public const string NotificationScheduledAtMustBeFuture = "NOTIFICATION_SCHEDULED_AT_MUST_BE_FUTURE";

        // ============ PRODUCT ============
        public const string ProductDuplicateUnitInPriceTiers = "PRODUCT_DUPLICATE_UNIT_IN_PRICE_TIERS";
        public const string ProductDuplicateSku = "PRODUCT_DUPLICATE_SKU";
        public const string ProductCannotChangeLocation = "PRODUCT_CANNOT_CHANGE_LOCATION";
        public const string ProductInvalidStatus = "PRODUCT_INVALID_STATUS";
        public const string ProductInitialStockMemo = "PRODUCT_INITIAL_STOCK_MEMO";
        public const string ProductStockUpdatedOnUpdateMemo = "PRODUCT_STOCK_UPDATED_ON_UPDATE_MEMO";

        // ============ IMPORT ============
        public const string ImportConfirmedSuccessfully = "IMPORT_CONFIRMED";
        public const string ImportOnlyDraftCanBeEdited = "IMPORT_ONLY_DRAFT_CAN_BE_EDITED";
        public const string ImportDateRequiredOnConfirm = "IMPORT_DATE_REQUIRED_ON_CONFIRM";
        public const string ImportProductNotFound = "IMPORT_PRODUCT_NOT_FOUND";
        public const string ImportLocationNotFound = "IMPORT_LOCATION_NOT_FOUND";
        public const string ImportAlreadyCancelled = "IMPORT_ALREADY_CANCELLED";
        public const string ImportItemsRequiredOnConfirm = "IMPORT_ITEMS_REQUIRED_ON_CONFIRM";
        public const string LowStockConfirmRequired = "LOW_STOCK_CONFIRM_REQUIRED";
        public const string OrderPaymentAmountMismatch = "ORDER_PAYMENT_AMOUNT_MISMATCH";
        public const string OrderLocationChangeNotAllowed = "ORDER_LOCATION_CHANGE_NOT_ALLOWED";
        /// <summary>Order was already completed or is not pending (e.g. duplicate complete after row lock).</summary>
        public const string OrderCompleteNotPending = "ORDER_COMPLETE_NOT_PENDING";

        // ============ IMPORT SCHEMA ============
        public const string ImportSchemaCannotDeleteActive = "IMPORT_SCHEMA_CANNOT_DELETE_ACTIVE";
        public const string ImportSchemaTemplateCodeConflict = "IMPORT_SCHEMA_TEMPLATE_CODE_CONFLICT";

        // ============ ACCOUNTING PERIOD ============
        public const string PeriodCreatedSuccessfully = "PERIOD_CREATED";
        public const string PeriodFinalizedSuccessfully = "PERIOD_FINALIZED";
        public const string PeriodReopenedSuccessfully = "PERIOD_REOPENED";
        public const string PeriodNotFound = "PERIOD_NOT_FOUND";
        public const string PeriodAlreadyExists = "PERIOD_ALREADY_EXISTS";
        public const string PeriodNotOpen = "PERIOD_NOT_OPEN";
        public const string PeriodAlreadyFinalized = "PERIOD_ALREADY_FINALIZED";
        public const string PeriodNotFinalized = "PERIOD_NOT_FINALIZED";
        public const string PeriodReopenReasonRequired = "PERIOD_REOPEN_REASON_REQUIRED";
        public const string PeriodNoBooks = "PERIOD_NO_BOOKS";
        public const string PeriodOpeningBalanceRequired = "PERIOD_OPENING_BALANCE_REQUIRED";
        public const string PeriodSuggestionNoSource = "PERIOD_SUGGESTION_NO_SOURCE";
        public const string PeriodSuggestionFromPrevious = "PERIOD_SUGGESTION_FROM_PREVIOUS";
        public const string PeriodSuggestionFormula = "PERIOD_SUGGESTION_FORMULA";
        // ============ DASHBOARD (user) ============
        public const string DashboardInvalidPeriod = "DASHBOARD_INVALID_PERIOD";
        public const string DashboardCustomDatesRequired = "DASHBOARD_CUSTOM_DATES_REQUIRED";

        // ============ DEBTOR ============
        public const string DebtorNotFound = "DEBTOR_NOT_FOUND";
        public const string DebtorNotActive = "DEBTOR_NOT_ACTIVE";
        public const string DebtorPhoneDuplicate = "DEBTOR_PHONE_DUPLICATE";
        public const string DebtorHasOutstandingBalance = "DEBTOR_HAS_OUTSTANDING_BALANCE";
        public const string DebtorPaymentAmountZero = "DEBTOR_PAYMENT_AMOUNT_ZERO";
        public const string DebtorPaymentMethodInvalid = "DEBTOR_PAYMENT_METHOD_INVALID";
        public const string DebtorCreditLimitExceededConfirmRequired = "DEBTOR_CREDIT_LIMIT_EXCEEDED_CONFIRM_REQUIRED";

        // ============ SUBSCRIPTION & PAYMENT ============
        public const string SubscriptionAlreadyActive = "SUBSCRIPTION_ALREADY_ACTIVE";
        public const string NoActiveSubscription = "SUBSCRIPTION_NO_ACTIVE_SUBSCRIPTION";
        public const string SubscriptionPlanNotFound = "SUBSCRIPTION_PLAN_NOT_FOUND";
        public const string PlanStripePriceNotConfigured = "SUBSCRIPTION_PLAN_STRIPE_PRICE_NOT_CONFIGURED";
        public const string StripeSecretMissing = "SUBSCRIPTION_STRIPE_SECRET_MISSING";
        public const string StripePriceNotConfigured = "SUBSCRIPTION_STRIPE_PRICE_NOT_CONFIGURED";
        public const string InvalidWebhookSignature = "SUBSCRIPTION_INVALID_WEBHOOK_SIGNATURE";
        public const string LocationIdRequired = "SUBSCRIPTION_LOCATION_ID_REQUIRED";
        public const string InvalidSubscriptionPlanId = "SUBSCRIPTION_INVALID_PLAN_ID";
        public const string SubscriptionCheckoutQuantityInvalid = "SUBSCRIPTION_CHECKOUT_QUANTITY_INVALID";
        public const string SubscriptionFreePlanPaymentNotAllowed = "SUBSCRIPTION_FREE_PLAN_PAYMENT_NOT_ALLOWED";
        public const string SubscriptionFeatureAccessDenied = "SUBSCRIPTION_FEATURE_ACCESS_DENIED";
        public const string SubscriptionFeatureUsageLimitReached = "SUBSCRIPTION_FEATURE_USAGE_LIMIT_REACHED";
        public const string SubscriptionAccessGrantMemberInvalid = "SUBSCRIPTION_ACCESS_GRANT_MEMBER_INVALID";
        public const string SubscriptionPlanDeleteHasActiveSubscriptions = "SUBSCRIPTION_PLAN_DELETE_HAS_ACTIVE_SUBSCRIPTIONS";
        public const string SubscriptionPlanActivateRequiresPrice = "SUBSCRIPTION_PLAN_ACTIVATE_REQUIRES_PRICE";
        public const string SubscriptionPlanActivateRequiresFeatures = "SUBSCRIPTION_PLAN_ACTIVATE_REQUIRES_FEATURES";
        public const string SubscriptionPlanFeatureNotFound = "SUBSCRIPTION_PLAN_FEATURE_NOT_FOUND";
        public const string SubscriptionActivatedTitle = "SUBSCRIPTION_ACTIVATED_TITLE";
        public const string SubscriptionActivatedBody = "SUBSCRIPTION_ACTIVATED_BODY";
        public const string PaymentFailedTitle = "SUBSCRIPTION_PAYMENT_FAILED_TITLE";
        public const string PaymentFailedBody = "SUBSCRIPTION_PAYMENT_FAILED_BODY";
        public const string SubscriptionExpiringTitle = "SUBSCRIPTION_EXPIRING_TITLE";
        public const string SubscriptionExpiringBody = "SUBSCRIPTION_EXPIRING_BODY";
        public const string SubscriptionExpiredTitle = "SUBSCRIPTION_EXPIRED_TITLE";
        public const string SubscriptionExpiredBody = "SUBSCRIPTION_EXPIRED_BODY";
        public const string SubscriptionEmployeeExpiringTitle = "SUBSCRIPTION_EMPLOYEE_EXPIRING_TITLE";
        public const string SubscriptionEmployeeExpiringBody = "SUBSCRIPTION_EMPLOYEE_EXPIRING_BODY";
        public const string SubscriptionDiscountStartInPast = "SUBSCRIPTION_DISCOUNT_START_IN_PAST";
        public const string SubscriptionDiscountEndInPast = "SUBSCRIPTION_DISCOUNT_END_IN_PAST";
        public const string SubscriptionDiscountEndMustBeAfterStart = "SUBSCRIPTION_DISCOUNT_END_MUST_BE_AFTER_START";

        // ============ AI SERVICE ============
        public const string AiServiceError = "AI_SERVICE_ERROR";

        // ============ ADMIN ============
        public const string AdminOnly = "ADMIN_ONLY";

        // =========================================================================================
        // OTP VERIFICATION
        // =========================================================================================
        public const string OtpSent            = "OTP_SENT";
        public const string OtpVerified         = "OTP_VERIFIED";
        public const string OtpInvalidOrExpired = "OTP_INVALID_OR_EXPIRED";
        public const string OtpTooManyRequests  = "OTP_TOO_MANY_REQUESTS";
    }
}
