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
    }
}
