using backend.Common;

namespace backend.Utils.Errors
{
    public static class StandardErrors
    {
        // Core error categories
        public static readonly Error NotFound = new("NOT_FOUND", "Requested entity not found.");
        public static readonly Error InvalidInput = new("INVALID_INPUT", "Invalid input data.");
        public static readonly Error Unauthorized = new("UNAUTHORIZED", "User is not authorized.");
        public static readonly Error AlreadyExists = new("ALREADY_EXISTS", "Entity already exists.");
        public static readonly Error InvalidAction = new("INVALID_ACTION", "Invalid operation for current entity state.");

        public static readonly Error TransactionErrorAdd = new("TRANSACTION_ERROR_ADD", "Cannot add credits to user");
        public static readonly Error TransactionErrorSpend = new("TRANSACTION_ERROR_SPEND", "Cannot spend credits");
    
        public static readonly Error InsufficientCredits = new("INSUFFICIENT_CREDITS", "Not enough credits to complete operation");

        // Contextual variants — same category, refined message
        public static readonly Error NonexistentBar = NotFound with { Message = "The specified bar does not exist." };
        public static readonly Error NonexistentUser = NotFound with { Message = "The specified user does not exist." };
        public static readonly Error NonexistentEntry = NotFound with { Message = "The specified entry does not exist." };
        public static readonly Error EntryAlreadyExists = AlreadyExists with { Message = "The specified entry already exists." };

        public static readonly Error InvalidBarAction = InvalidAction with { Message = "Cannot perform this action on a bar in its current state." };

         public static readonly Error NotFoundCredits = NotFound with { Message = "The credit amount cannot be retrieved" };

         public static readonly Error NotFoundPlaylist = NotFound with { Message = "Playlist was not found." };
    }
}
