namespace SRVS.Domain.Enums;

public enum UserRoleType
{
    Admin = 0,
    DepartmentHead = 1,
    Educator = 2,
    Viewer = 3
}

public enum UserAccountStatus
{
    PendingApproval = 0,
    Active = 1,
    Suspended = 2,
    Rejected = 3,
    Deleted = 4
}

public enum RegistrationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum SyllabusStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum AuditActionType
{
    LoginAttempt = 0,
    LoginSuccess = 1,
    LoginFailure = 2,
    Logout = 3,
    PasswordResetRequested = 4,
    PasswordResetCompleted = 5,
    RegistrationSubmitted = 6,
    RegistrationApproved = 7,
    RegistrationRejected = 8,
    SyllabusUploaded = 9,
    SyllabusSubmitted = 10,
    SyllabusApproved = 11,
    SyllabusRejected = 12,
    SyllabusRestored = 13,
    RoleUpdated = 14,
    AccountDeactivated = 15,
    AccountActivated = 16,
    AccountDeleted = 17
}

public enum AuditResultStatus
{
    Success = 0,
    Failed = 1,
    Warning = 2
}

public enum NotificationType
{
    RegistrationApproved = 0,
    RegistrationRejected = 1,
    SubmissionAlert = 2,
    ApprovalAlert = 3,
    RejectionAlert = 4,
    RevisionUploaded = 5,
    RestorationNotice = 6,
    SystemMessage = 7
}