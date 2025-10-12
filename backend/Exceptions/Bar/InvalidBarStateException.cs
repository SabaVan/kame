namespace backend.Exceptions.Bar;

using backend.Enums;
using System;

public class InvalidBarStateException : Exception
{
    public InvalidBarStateException() { }
    public InvalidBarStateException(string message)
        : base(message) { }
}
