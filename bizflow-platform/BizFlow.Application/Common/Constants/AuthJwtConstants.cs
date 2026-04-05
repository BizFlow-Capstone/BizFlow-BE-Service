namespace BizFlow.Application.Common.Constants
{
    /// <summary>JWT claim values and authorization policy names for auth flows.</summary>
    public static class AuthJwtConstants
    {
        /// <summary>Custom claim: token purpose (e.g. password reset after email OTP).</summary>
        public const string PurposeClaimType = "purpose";

        public const string PasswordResetPurpose = "password_reset";

        /// <summary>Must match Account.PasswordResetNonce in DB until reset completes.</summary>
        public const string PasswordResetNonceClaimType = "pwd_reset_nonce";

        public static class Policies
        {
            public const string PasswordReset = "PasswordReset";
        }
    }
}
