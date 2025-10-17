namespace backend.Common
{
    public record Result<T>(bool IsSuccess, T? Value, Error? Error)
    {
        public bool IsFailure => !IsSuccess;
        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string code, string message) => new(false, default, new Error(code, message));
        public static Result<T> Failure(Error err) => new(false, default, err);
    }
}
