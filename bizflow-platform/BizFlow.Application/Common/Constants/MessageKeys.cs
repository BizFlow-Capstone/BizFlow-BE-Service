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
        public const string TokenRefreshed = "AUTH_TOKEN_REFRESHED";
        public const string InvalidGoogleToken = "AUTH_INVALID_GOOGLE_TOKEN";
        public const string InvalidFirebaseToken = "AUTH_INVALID_FIREBASE_TOKEN";
        public const string PhoneAlreadyExists = "AUTH_PHONE_ALREADY_EXISTS";
        public const string PhoneAlreadyLinked = "AUTH_PHONE_ALREADY_LINKED";
        public const string PhoneVerificationMismatch = "AUTH_PHONE_VERIFICATION_MISMATCH";
        public const string PhoneRegisterSuccess = "AUTH_PHONE_REGISTER_SUCCESS";
        public const string PhoneLinkSuccess = "AUTH_PHONE_LINK_SUCCESS";
        public const string AccountNotFound = "AUTH_ACCOUNT_NOT_FOUND";
        public const string LogoutAllSuccess = "AUTH_LOGOUT_ALL_SUCCESS";

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

        // ============ IMPORT SCHEMA ============
        public const string ImportSchemaCannotDeleteActive = "IMPORT_SCHEMA_CANNOT_DELETE_ACTIVE";
        public const string ImportSchemaTemplateCodeConflict = "IMPORT_SCHEMA_TEMPLATE_CODE_CONFLICT";

        // ============ DEBTOR ============
        public const string DebtorNotFound = "DEBTOR_NOT_FOUND";
        public const string DebtorNotActive = "DEBTOR_NOT_ACTIVE";
        public const string DebtorPhoneDuplicate = "DEBTOR_PHONE_DUPLICATE";
        public const string DebtorHasOutstandingBalance = "DEBTOR_HAS_OUTSTANDING_BALANCE";
        public const string DebtorPaymentAmountZero = "DEBTOR_PAYMENT_AMOUNT_ZERO";
        public const string DebtorPaymentMethodInvalid = "DEBTOR_PAYMENT_METHOD_INVALID";
    }
}
