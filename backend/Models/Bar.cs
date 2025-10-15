using backend.Enums;
using backend.Utils;
namespace backend.Models
{
    public class Bar
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public BarState State { get; private set; } = BarState.Closed;
        public TimeSpan OpenAt { get; set; }
        public TimeSpan CloseAt { get; set; }
        // Playlist CurrentPlaylist;
        public Bar()
        {

        }
        public Result<BarState> SetState(BarState newState)
        {
            if (State != newState)
            {
                State = newState;
                return Result<BarState>.Success(State);
            } else return Result<BarState>.Failure("BAR_ALREADY_IN_STATE", $"Bar is already in state {State}");         
        }
    }
}