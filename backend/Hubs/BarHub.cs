using backend.Shared.DTOs;
using backend.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using backend.Repositories.Interfaces;

namespace backend.Hubs
{
    public class BarHub : Hub
    {
        public BarHub()
        {
        }

        // Add connection to bar group and broadcast update
        public async Task JoinBarGroup(Guid barId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, barId.ToString());
            await Clients.Group(barId.ToString()).SendAsync("BarUsersUpdated");
        }

        public async Task LeaveBarGroup(Guid barId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, barId.ToString());
            await Clients.Group(barId.ToString()).SendAsync("BarUsersUpdated");
        }
    }
}
