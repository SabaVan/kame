using backend.Shared.DTOs;
using backend.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using backend.Repositories.Interfaces;
using System.Collections.Concurrent;

namespace backend.Hubs
{
    /// <summary>
    /// Manages real-time bar updates and user presence tracking using SignalR.
    /// Uses ConcurrentDictionary to safely track which users are connected to which bars
    /// in a multi-threaded environment.
    /// </summary>
    public class BarHub : Hub
    {
        // Thread-safe collection mapping bar IDs to sets of active connection IDs
        // Key: Bar ID as string
        // Value: ConcurrentBag of connection IDs currently in that bar
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> ActiveBarConnections = new();

        public BarHub()
        {
        }

        /// <summary>
        /// Adds a user connection to a bar group and tracks it in ActiveBarConnections.
        /// Thread-safe: Multiple concurrent join requests are handled safely without race conditions.
        /// </summary>
        public async Task JoinBarGroup(Guid barId)
        {
            string barIdStr = barId.ToString();
            string connectionId = Context.ConnectionId;

            // Add connection to SignalR group
            await Groups.AddToGroupAsync(connectionId, barIdStr);

            // Add to concurrent tracking dictionary
            // AddOrUpdate: either creates new ConcurrentBag with this connection, or adds to existing one
            ActiveBarConnections.AddOrUpdate(
                barIdStr,
                new ConcurrentBag<string> { connectionId },
                (key, existingBag) =>
                {
                    existingBag.Add(connectionId);
                    return existingBag;
                }
            );

            // Notify all users in this bar that someone joined
            await Clients.Group(barIdStr).SendAsync("BarUsersUpdated", new { connectionId });
        }

        /// <summary>
        /// Removes a user connection from a bar group and updates tracking.
        /// Thread-safe: Even if multiple users are leaving concurrently, no state corruption occurs.
        /// </summary>
        public async Task LeaveBarGroup(Guid barId)
        {
            string barIdStr = barId.ToString();
            string connectionId = Context.ConnectionId;

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(connectionId, barIdStr);

            // Remove from concurrent tracking dictionary
            RemoveConnectionFromBar(barIdStr, connectionId);

            // Notify remaining users in this bar
            await Clients.Group(barIdStr).SendAsync("BarUsersUpdated", new { connectionId });
        }

        /// <summary>
        /// Called when a client disconnects unexpectedly (connection lost, tab closed, etc.)
        /// Ensures cleanup from all bars they were in.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connectionId = Context.ConnectionId;

            // Find all bars this connection was in and remove it
            // We iterate over a snapshot to avoid modification during iteration
            foreach (var barEntry in ActiveBarConnections.ToList())
            {
                RemoveConnectionFromBar(barEntry.Key, connectionId);
                // Notify remaining users in affected bars
                await Clients.Group(barEntry.Key).SendAsync("BarUsersUpdated", new { connectionId });
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Helper method to safely remove a connection from a bar.
        /// Uses TryUpdateAsync pattern to ensure thread-safe removal.
        /// </summary>
        private void RemoveConnectionFromBar(string barId, string connectionId)
        {
            if (ActiveBarConnections.TryGetValue(barId, out var connections))
            {
                // Create a new ConcurrentBag without this connection
                var remaining = new ConcurrentBag<string>(
                    connections.Where(c => c != connectionId)
                );

                if (remaining.Count == 0)
                {
                    // If no connections left, remove the bar entirely
                    ActiveBarConnections.TryRemove(barId, out _);
                }
                else
                {
                    // Update with the new bag
                    ActiveBarConnections.TryUpdate(barId, remaining, connections);
                }
            }
        }

        /// <summary>
        /// Gets the current count of active connections in a bar.
        /// Useful for debugging and metrics.
        /// </summary>
        public int GetBarConnectionCount(Guid barId)
        {
            string barIdStr = barId.ToString();
            if (ActiveBarConnections.TryGetValue(barIdStr, out var connections))
            {
                return connections.Count;
            }
            return 0;
        }

        /// <summary>
        /// Gets all active bar IDs with connections.
        /// Useful for monitoring which bars have active users.
        /// </summary>
        public IEnumerable<Guid> GetActiveBars()
        {
            return ActiveBarConnections.Keys.Select(k => Guid.Parse(k));
        }
    }
}
