using backend.Enums;

namespace backend.Exceptions.Bar
{
    public class IllegalBarActionException : Exception
    {
        public IllegalBarActionException(string action, BarState state)
        : base($"Cannot perform '{action}' while the bar is {state}.") {}
    }
}