namespace backend.Utils
{
    public record Result<T>(bool IsSuccess, T? Value, Error? Error)
    {
        public bool IsFailure => !IsSuccess;
        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string code, string message) => new(false, default, new Error(code, message));
        public static Result<T> Failure(Error err) => new(false, default, err);
    }
}
/* For example
Result<int> success = Result<int>.Success(42);
Console.WriteLine(success.IsSuccess); // True

Result<int> failure = Result<int>.Failure("NOT_FOUND", "Item not found");
Console.WriteLine(failure.Value);     // null
Console.WriteLine(failure.IsFailure); // True
Console.WriteLine(failure.Error?.Message); // "Item not found" */