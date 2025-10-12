namespace backend.Records
{
    public record Result<T>
    {
        public bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        // Factory method for success
        public static Result<T> Success(T value) =>
            new Result<T> { IsSuccess = true, Value = value };

        // Factory method for failure
        public static Result<T> Failure(string code, string message) =>
            new Result<T> { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
    }
}
