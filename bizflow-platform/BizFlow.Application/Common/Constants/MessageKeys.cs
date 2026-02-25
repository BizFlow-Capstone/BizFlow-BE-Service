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
        public const string ValidationError = "COMMON_VALIDATION_ERROR";
        public const string DataRetrievedSuccessfully = "COMMON_DATA_RETRIEVED";
        public const string DataCreatedSuccessfully = "COMMON_DATA_CREATED";
        public const string DataUpdatedSuccessfully = "COMMON_DATA_UPDATED";
        public const string DataDeletedSuccessfully = "COMMON_DATA_DELETED";

        // ============ AUTHENTICATION ============
        public const string LoginSuccess = "AUTH_LOGIN_SUCCESS";
        public const string LoginFailed = "AUTH_LOGIN_FAILED";
        public const string LogoutSuccess = "AUTH_LOGOUT_SUCCESS";
        public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
        public const string InvalidToken = "AUTH_INVALID_TOKEN";
        public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";

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
        public const string LocationNotFound = "LOCATION_NOT_FOUND";
        public const string LocationCreatedSuccessfully = "LOCATION_CREATED";
        public const string LocationUpdatedSuccessfully = "LOCATION_UPDATED";
        public const string LocationDeletedSuccessfully = "LOCATION_DELETED";
        public const string LocationsRetrievedSuccessfully = "LOCATION_RETRIEVED";
        public const string LocationStatusUpdated = "LOCATION_STATUS_UPDATED";
        public const string LocationAccessDenied = "LOCATION_ACCESS_DENIED";
        public const string LocationAlreadyExists = "LOCATION_ALREADY_EXISTS";
        public const string EmployeesRetrievedSuccessfully = "LOCATION_EMPLOYEES_RETRIEVED";

        // ============ HIRE ============
        public const string HireEmployeesRetrievedSuccessfully = "HIRE_EMPLOYEES_RETRIEVED";
        public const string EmployeesNotHired = "HIRE_EMPLOYEES_NOT_HIRED";
        public const string EmployeesAlreadyAssigned = "LOCATION_EMPLOYEES_ALREADY_ASSIGNED";
        public const string EmployeesAddedSuccessfully = "LOCATION_EMPLOYEES_ADDED";

        // ============ PRODUCT ============
        public const string ProductNotFound = "PRODUCT_NOT_FOUND";
        public const string ProductsRetrievedSuccessfully = "PRODUCT_RETRIEVED";
        public const string ProductCreatedSuccessfully = "PRODUCT_CREATED";
        public const string ProductStatusUpdated = "PRODUCT_STATUS_UPDATED";
        public const string ProductAccessDenied = "PRODUCT_ACCESS_DENIED";
        public const string ProductDeletedSuccessfully = "PRODUCT_DELETED";
        public const string ProductDuplicateUnitInPriceTiers = "PRODUCT_DUPLICATE_UNIT_IN_PRICE_TIERS";
        public const string ProductImageUploadFailed = "PRODUCT_IMAGE_UPLOAD_FAILED";
        public const string ProductUpdatedSuccessfully = "PRODUCT_UPDATED";
        public const string ProductCannotChangeLocation = "PRODUCT_CANNOT_CHANGE_LOCATION";
        public const string ProductInvalidStatus = "PRODUCT_INVALID_STATUS";

        // ============ IMPORT ============
        public const string ImportNotFound = "IMPORT_NOT_FOUND";
        public const string ImportRetrievedSuccessfully = "IMPORT_RETRIEVED";
        public const string ImportCreatedSuccessfully = "IMPORT_CREATED";
        public const string ImportUpdatedSuccessfully = "IMPORT_UPDATED";
        public const string ImportConfirmedSuccessfully = "IMPORT_CONFIRMED";
        public const string ImportCancelledSuccessfully = "IMPORT_CANCELLED";
        public const string ImportDeletedSuccessfully = "IMPORT_DELETED";
        public const string ImportTemplateRetrievedSuccessfully = "IMPORT_TEMPLATE_RETRIEVED";
        public const string ImportOnlyDraftCanBeEdited = "IMPORT_ONLY_DRAFT_CAN_BE_EDITED";
        public const string ImportOnlyDraftCanBeDeleted = "IMPORT_ONLY_DRAFT_CAN_BE_DELETED";
        public const string ImportInvalidAction = "IMPORT_INVALID_ACTION";
        public const string ImportDateRequiredOnConfirm = "IMPORT_DATE_REQUIRED_ON_CONFIRM";
        public const string ImportProductNotFound = "IMPORT_PRODUCT_NOT_FOUND";
        public const string ImportLocationNotFound = "IMPORT_LOCATION_NOT_FOUND";
        public const string ImportAlreadyCancelled = "IMPORT_ALREADY_CANCELLED";

        // ============ IMPORT SCHEMA ============
        public const string ImportSchemaNotFound = "IMPORT_SCHEMA_NOT_FOUND";
        public const string ImportSchemaRetrieved = "IMPORT_SCHEMA_RETRIEVED";
        public const string ImportSchemaCreated = "IMPORT_SCHEMA_CREATED";
        public const string ImportSchemaUpdated = "IMPORT_SCHEMA_UPDATED";
        public const string ImportSchemaActivated = "IMPORT_SCHEMA_ACTIVATED";
        public const string ImportSchemaDeleted = "IMPORT_SCHEMA_DELETED";
        public const string ImportSchemaCannotDeleteActive = "IMPORT_SCHEMA_CANNOT_DELETE_ACTIVE";
        public const string ImportSchemaTemplateCodeConflict = "IMPORT_SCHEMA_TEMPLATE_CODE_CONFLICT";
    }
}
