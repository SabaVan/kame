using backend.Utils;

namespace backend.Utils.Errors
{
    public static class StandardErrors
    {
        public static readonly Error NotFound = new("NOT_FOUND", "Requested entity not found");
        public static readonly Error InvalidInput = new("INVALID_INPUT", "Invalid input data");
        public static readonly Error Unauthorized = new("UNAUTHORIZED", "User is not authorized");
        public static readonly Error InsufficientCredits = new("INSUFFICIENT_CREDITS", "Not enough credits to complete operation");
    }
}
