namespace SaveState.Core.Common.Constants;

/// <summary>
/// Validation error messages used in FluentValidation and data annotations.
/// </summary>
public static class ValidationMessages
{
    // Required Fields
    public const string Required = "{PropertyName} is required";
    public const string RequiredField = "This field is required";
    public const string RequiredSelection = "Please select a value";
    public const string RequiredFile = "Please select a file";

    // String Length
    public const string MinLength = "{PropertyName} must be at least {MinLength} characters";
    public const string MaxLength = "{PropertyName} must not exceed {MaxLength} characters";
    public const string ExactLength = "{PropertyName} must be exactly {MaxLength} characters";
    public const string LengthBetween = "{PropertyName} must be between {MinLength} and {MaxLength} characters";

    // Numeric Range
    public const string GreaterThan = "{PropertyName} must be greater than {ComparisonValue}";
    public const string GreaterThanOrEqual = "{PropertyName} must be at least {ComparisonValue}";
    public const string LessThan = "{PropertyName} must be less than {ComparisonValue}";
    public const string LessThanOrEqual = "{PropertyName} must not exceed {ComparisonValue}";
    public const string Range = "{PropertyName} must be between {From} and {To}";
    public const string PositiveNumber = "{PropertyName} must be a positive number";
    public const string NonNegativeNumber = "{PropertyName} must be zero or greater";

    // Format Validation
    public const string InvalidEmail = "Please enter a valid email address";
    public const string InvalidUrl = "Please enter a valid URL";
    public const string InvalidGuid = "Please enter a valid identifier";
    public const string InvalidDate = "Please enter a valid date";
    public const string InvalidTime = "Please enter a valid time";
    public const string InvalidDateTime = "Please enter a valid date and time";
    public const string InvalidRegex = "{PropertyName} format is invalid";

    // Comparison
    public const string MustMatch = "{PropertyName} must match {ComparisonProperty}";
    public const string MustNotMatch = "{PropertyName} must not match {ComparisonProperty}";
    public const string MustBeDifferent = "{PropertyName} must be different from {ComparisonProperty}";
    public const string MustBeUnique = "{PropertyName} must be unique";

    // Collection Validation
    public const string MinItems = "At least {MinItems} item(s) required";
    public const string MaxItems = "No more than {MaxItems} item(s) allowed";
    public const string ExactItems = "Exactly {Count} item(s) required";
    public const string EmptyCollection = "{PropertyName} cannot be empty";
    public const string DuplicateItem = "Duplicate items are not allowed";

    // File Validation
    public const string InvalidFileType = "File type not allowed. Allowed types: {AllowedTypes}";
    public const string FileTooLarge = "File size exceeds maximum allowed ({MaxSizeMB}MB)";
    public const string FileTooSmall = "File size is below minimum required ({MinSizeKB}KB)";
    public const string InvalidImage = "Invalid image file";

    // Authentication
    public const string WeakPassword = "Password must contain at least 8 characters, one uppercase, one lowercase, one number, and one special character";
    public const string PasswordsDoNotMatch = "Passwords do not match";
    public const string InvalidUsername = "Username can only contain letters, numbers, and underscores";
    public const string UsernameTaken = "Username is already taken";
    public const string EmailTaken = "Email address is already registered";

    // Specific Domain Validations
    public const string InvalidGamePath = "Invalid game path";
    public const string InvalidRomPath = "Invalid ROM file path";
    public const string InvalidEmulatorPath = "Invalid emulator path";
    public const string InvalidSaveStatePath = "Invalid save state path";
    public const string InvalidMugenPath = "Invalid MUGEN installation path";
    public const string InvalidRating = "Rating must be between {Min} and {Max}";

    // General
    public const string InvalidValue = "Invalid value for {PropertyName}";
    public const string InvalidOperation = "This operation is not valid";
    public const string AlreadyExists = "{PropertyName} already exists";
    public const string NotFound = "{PropertyName} not found";
    public const string MustBeEmpty = "{PropertyName} must be empty";
    public const string MustNotBeEmpty = "{PropertyName} must not be empty";
    public const string MustBeNull = "{PropertyName} must be null";
    public const string MustNotBeNull = "{PropertyName} must not be null";
    public const string InvalidEnumValue = "Invalid value for {PropertyName}";
    public const string Custom = "{PropertyName} is invalid";
}
