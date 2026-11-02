namespace Keyloop.Application.Commons;

public class Result
{
    public bool IsSuccess { get; protected init; }
    public string? Error { get; protected init; }
    public string? Code { get; protected init; }

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(string error, string? code = null) =>
        new() { IsSuccess = false, Error = error, Code = code };

    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);

    public static Result<T> Failure<T>(string error, string? code = null) =>
        Result<T>.Fail(error, code);
}

public class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };

    public static Result<T> Fail(string error, string? code = null) =>
        new() { IsSuccess = false, Error = error, Code = code };
}
