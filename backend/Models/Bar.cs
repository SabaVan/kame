using backend.Enums;
using backend.Exceptions.Bar;
namespace backend.Models
{
    public class Bar
    {
        public Guid Id { get; set; }
        public BarState State { get; private set; } = BarState.Closed;
        public TimeSpan OpenAt { get; set; }
        public TimeSpan CloseAt { get; set; }
        // Playlist CurrentPlaylist;
        public Bar()
        {

        }
        public void SetState(BarState newState)
        {
            if (State != newState)
                State = newState;
            // else return new Result(false, null, $"The bar is already: {State}") ..            
        }
    }
}