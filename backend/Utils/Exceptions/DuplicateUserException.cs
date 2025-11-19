using System;

namespace backend.Exceptions
{
    public class DuplicateUserException : Exception
    {
        public string Username { get; }

        public DuplicateUserException(string username)
            : base($"Username '{username}' is already taken.")
        {
            Username = username;
        }

        public DuplicateUserException(string username, Exception inner)
            : base($"Username '{username}' is already taken.", inner)
        {
            Username = username;
        }
    }
}
