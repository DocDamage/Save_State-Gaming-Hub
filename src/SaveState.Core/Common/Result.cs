namespace SaveState.Core.Common;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail.
/// Use this for operations that don't return a value.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed. Opposite of <see cref="IsSuccess"/>.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed; otherwise null.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the type of error that occurred.
    /// </summary>
    public ErrorType ErrorType { get; }

    /// <summary>
    /// Creates a new Result instance.
    /// </summary>
    protected Result(bool isSuccess, string? error = null, ErrorType errorType = ErrorType.None)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorType">The type of error (defaults to Validation).</param>
    public static Result Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, error, errorType);
}

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value if the operation succeeded; otherwise default.
    /// Always check <see cref="Result.IsSuccess"/> before accessing.
    /// </summary>
    public T? Value { get; }

    private Result(bool isSuccess, T? value = default, string? error = null, ErrorType errorType = ErrorType.None)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorType">The type of error (defaults to Validation).</param>
    public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, default, error, errorType);
}

/// <summary>
/// Categorizes the type of error that occurred in an operation.
/// </summary>
public enum ErrorType
{
    /// <summary>No error occurred.</summary>
    None,
    /// <summary>Input validation failed.</summary>
    Validation,
    /// <summary>Requested resource was not found.</summary>
    NotFound,
    /// <summary>Operation conflicts with existing state.</summary>
    Conflict,
    /// <summary>User is not authenticated.</summary>
    Unauthorized,
    /// <summary>User lacks permission for this operation.</summary>
    Forbidden,
    /// <summary>Internal server/application error.</summary>
    Internal,
    /// <summary>External service (API, database) failed.</summary>
    ExternalService,
    /// <summary>Feature is not yet implemented.</summary>
    NotImplemented
}

