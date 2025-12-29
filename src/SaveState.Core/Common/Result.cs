namespace SaveState.Core.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, string? error = null, ErrorType errorType = ErrorType.None)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, error, errorType);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value = default, string? error = null, ErrorType errorType = ErrorType.None)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, default, error, errorType);
}

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Internal
}
