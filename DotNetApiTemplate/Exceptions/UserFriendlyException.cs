
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

    #region GENERAL

    Unauthorized = 25,
    Forbidden = 26,
    InvalidRequest = 27,
    MissingRequiredFields = 28,
    InvalidInput = 29,
    ResourceNotFound = 30,
    OperationFailed = 31,

    #endregion

    #region BRANCHES

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
    EmployeeAlreadyAssigned = 43,
    EmployeeNotFound = 44,

    #endregion

    #region WAREHOUSES

    BranchIdsIsRequired = 45,
    WarehouseManagerIdIsRequired = 46,
    BranchAlreadyAssigned = 47,
    WarehouseNotFound = 48,
    WarehouseManagerIsRequired = 49,
    WarehouseNeedsToHaveAtLeastOneEmployee = 50,

    #endregion

    #region BRANCH WORKERS

    BranchWorkerNotFound = 51,
    BranchWorkerAlreadyAssigned = 52,
    BranchWorkerInactive = 53,
    BranchWorkerBelongsToAnotherBranch = 54,
    BranchWorkerCannotBeBranchManager = 55,
    BranchManagerCannotBeUnassigned = 56,
    UserAlreadyAssignedToBranch = 57,
    UserAlreadyAssignedToWarehouse = 58,

    #endregion

    #region WAREHOUSE MANAGERS

    WarehouseManagerUserIdIsRequired = 59,
    WarehouseManagerNotFound = 60,
    WarehouseManagerAlreadyAssigned = 61,
    WarehouseManagerAlreadyManagesWarehouse = 62,
    WarehouseManagerCannotBeBranchWorker = 63,
    WarehouseManagerCannotBeAdmin = 64,
    WarehouseManagerRoleAssignmentFailed = 65,

    #endregion

    #region WAREHOUSE WORKERS

    WarehouseWorkerNotFound = 66,
    WarehouseWorkerAlreadyAssigned = 67,
    WarehouseWorkerInactive = 68,
    WarehouseWorkerBelongsToAnotherWarehouse = 69,
    WarehouseNeedsAtLeastOneActiveWorker = 70,

    #endregion

    #region WAREHOUSE VALIDATION

    WarehouseAlreadyExists = 71,
    WarehouseCodeIsRequired = 72,
    WarehouseCodeGenerationFailed = 73,
    WarehouseInactive = 74,
    WarehouseAlreadyActive = 75,
    WarehouseAlreadyInactive = 76,
    WarehouseHasNoManager = 77,

    #endregion
}
