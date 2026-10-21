
namespace DotNetApiTemplate.Exceptions;

public class UserFriendlyException(ErrorMessages error)
    : Exception(error.ToString());

public enum ErrorMessages
{
    #region ACCOUNT
    UserNotFound = 1,
    UserAlreadyExists = 2,
    EmailAlreadyExists = 3,
    InvalidEmail = 4,
    InvalidCredentials = 5,
    InvalidPassword = 6,
    IncorrectPassword = 7,
    PasswordsDoNotMatch = 8,
    PasswordTooWeak = 9,
    NewPasswordMustBeDifferent = 10,
    AccountDisabled = 11,
    AccountLocked = 12,

    EmailNotConfirmed = 13,
    EmailAlreadyConfirmed = 14,
    InvalidConfirmationCode = 15,
    ConfirmationCodeExpired = 16,

    InvalidResetToken = 17,
    ResetTokenExpired = 18,
    PasswordResetFailed = 19,

    InvalidRefreshToken = 20,
    RefreshTokenExpired = 21,
    RefreshTokenRevoked = 22,
    InvalidAccessToken = 23,
    AccessTokenExpired = 24,

    #endregion
    Unauthorized = 25,
    Forbidden = 26,
    InvalidRequest = 27,
    MissingRequiredFields = 28,
    InvalidInput = 29,
    ResourceNotFound = 30,
    OperationFailed = 31,
    BranchAlreadyExists = 32,
    InvalidBranchName = 33,
    BranchNotFound = 34,
    BranchCodeGenerationFailed = 35,
    BranchCodeIsRequired = 36,
    UserIsNotBranchManager = 37,
    ManagerAlreadyAssigned = 38,
    FailedToAssignRole = 39,
    FailedToRemoveRole = 40,

     NoAssignedEmployees = 41,
    InvalidPagination = 42,
    EmployeeAlreadyAssigned=43



}
