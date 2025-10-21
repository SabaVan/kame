using backend.Shared.Enums;
using backend.Common;
namespace backend.Models
{
    public class Bar
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public BarState State { get; private set; } = BarState.Closed;
        public DateTime OpenAtUtc { get; private set; }
        public DateTime CloseAtUtc { get; private set; }
        public Playlist? CurrentPlaylist { get; set; } = null;
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
            }
            else return Result<BarState>.Failure("BAR_ALREADY_IN_STATE", $"Bar is already in state {State}");
        }
        public bool ShouldBeOpen(DateTime nowUtc)
        {
            // Convert to TimeSpan for comparison
            TimeSpan nowTime = nowUtc.TimeOfDay;
            TimeSpan openTime = OpenAtUtc.TimeOfDay;
            TimeSpan closeTime = CloseAtUtc.TimeOfDay;

            if (openTime < closeTime)
            {
                // Normal: same day
                return nowTime >= openTime && nowTime < closeTime;
            }
            else
            {
                // Overnight: e.g., 17:00 - 01:00
                return nowTime >= openTime || nowTime < closeTime;
            }
        }

        public Result<bool> SetSchedule(DateTime open, DateTime close)
        {
            if (open >= close) return Result<bool>.Failure("INVALID_SCHEDULE", "Open time must be before close time");
            OpenAtUtc = open;
            CloseAtUtc = close;
            return Result<bool>.Success(true);
        }
    }

    public class Playlist
    {
        public Guid Id { set; get; } = Guid.NewGuid();
    }
}