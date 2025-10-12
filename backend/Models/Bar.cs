using backend.Enums;
using backend.Exceptions.Bar;
namespace backend.Models
{
    public class Bar
    {
        public Guid Id { get; set; }
        public BarState State { get; private set; } = BarState.Closed;
        public Bar()
        {

        }
        public void SetState(BarState newState)
        {
            if (State != newState)
                State = newState;
            else throw new InvalidBarStateException($"The bar's state is already: {State}");
        }
    }
}