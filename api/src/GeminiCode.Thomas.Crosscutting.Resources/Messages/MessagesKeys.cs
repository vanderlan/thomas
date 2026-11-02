namespace GeminiCode.Thomas.Crosscutting.Resources.Messages;

/// <summary>
/// Update all resource files to maintain globalization support
/// </summary>
public static class MessagesKeys
{
    public static class ExceptionsTitles
    {
        public const string ValidationError = "ExceptionTitle.ValidationError";
        public const string AuthenticationError = "Authentication.Error";
    }

    public static class UserMessages
    {
        //Auth
        public const string InvalidCredentials = "User.InvalidCredentials";
        public const string InvalidRefreshToken = "User.InvalidRefreshToken";
        public const string RefreshTokenRotationFailed = "User.RefreshTokenRotationFailed";

        //User
        public const string NullEntity = "User.NullEntity";
        public const string EmptyName = "User.EmptyName";
        public const string EmptyPassword = "User.EmptyPassword";
        public const string EmptyEmail = "User.EmptyEmail";
        public const string EmptyId = "User.EmptyId";
        public const string EmailExists = "User.EmailExists";
        public const string InvalidPassword = "User.InvalidPassword";

        public const string EmptyNewPassword = "User.InvalidNewPassword";
        public const string EmptyNewPasswordConfirmation = "User.EmptyNewPasswordConfirmation";
        public const string PasswordAndConfirmationDifferent = "User.PasswordConfirmationDifferent";
    }

    public static class ProjectMessages
    {
        public const string NullEntity = "Project.NullEntity";
        public const string EmptyName = "Project.EmptyName";
        public const string EmptyNewRelicId = "Project.EmptyNewRelicId";
        public const string EmptyGitHubRepository = "Project.EmptyGitHubRepository";
        public const string InvalidGitHubRepository = "Project.InvalidGitHubRepository";
        public const string EmptyGitHubKeys = "Project.EmptyGitHubKeys";
        public const string EmptyGitHubAccessToken = "Project.EmptyGitHubAccessToken";
        public const string EmptyPriorityBranch = "Project.EmptyPriorityBranch";
        public const string NameExists = "Project.NameExists";
    }

    public static class ErrorLogMessages
    {
        public const string AlreadyResolved = "ErrorLog.AlreadyResolved";
    }
}
