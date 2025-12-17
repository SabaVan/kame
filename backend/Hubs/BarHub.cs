using backend.Shared.DTOs;
using backend.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using backend.Repositories.Interfaces;
using System.Collections.Concurrent;

namespace backend.Hubs
{
    public class BarHub : Hub
    {
        // Thread-safe collection mapping bar IDs to sets of active connection IDs
        // Key: Bar ID as string
        // Value: ConcurrentBag of connection IDs currently in that bar
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> ActiveBarConnections = new();
        // Map connectionId -> userId (if available from session)
        private static readonly ConcurrentDictionary<string, Guid> ConnectionUserMap = new();

        private readonly IBarUserEntryRepository _barUserEntryRepository;

        public BarHub(IBarUserEntryRepository barUserEntryRepository)
        {
            _barUserEntryRepository = barUserEntryRepository;
        }
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

            // Try to map this connection to a user id from session and persist presence
            try
            {
                var httpContext = Context.GetHttpContext();
                var userIdStr = httpContext?.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
                {
                    ConnectionUserMap[connectionId] = userId;

                    // Try to add entry; if it already exists, touch timestamp instead
                    var addResult = await _barUserEntryRepository.AddEntryAsync(barId, userId);
                    if (addResult.IsFailure)
                    {
                        await _barUserEntryRepository.TouchEntryAsync(barId, userId);
                    }
                    else
                    {
                        await _barUserEntryRepository.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // Swallow errors to avoid breaking real-time flow; presence persistence is best-effort
            }

            // Notify all users in this bar that someone joined
            await Clients.Group(barIdStr).SendAsync("BarUsersUpdated", new { connectionId });
        }

        public async Task LeaveBarGroup(Guid barId)
        {
            string barIdStr = barId.ToString();
            string connectionId = Context.ConnectionId;

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(connectionId, barIdStr);

            // Remove from concurrent tracking dictionary
            RemoveConnectionFromBar(barIdStr, connectionId);

            // If we knew the user for this connection, and there are no other connections for the same user in this bar,
            // remove the BarUserEntry from the database (user left all tabs/windows for that bar)
            try
            {
                if (ConnectionUserMap.TryRemove(connectionId, out var userId))
                {
                    int otherConnectionsForUser = 0;
                    if (ActiveBarConnections.TryGetValue(barIdStr, out var bag))
                    {
                        otherConnectionsForUser = bag.Count(c => ConnectionUserMap.TryGetValue(c, out var uid) && uid == userId);
                    }

                    if (otherConnectionsForUser == 0)
                    {
                        var removeResult = await _barUserEntryRepository.RemoveEntryAsync(barId, userId);
                        if (removeResult.IsSuccess)
                            await _barUserEntryRepository.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // best-effort
            }

            // Notify remaining users in this bar
            await Clients.Group(barIdStr).SendAsync("BarUsersUpdated", new { connectionId });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connectionId = Context.ConnectionId;

            // Find all bars this connection was in and remove it
            // We iterate over a snapshot to avoid modification during iteration
            foreach (var barEntry in ActiveBarConnections.ToList())
            {
                RemoveConnectionFromBar(barEntry.Key, connectionId);

                // If we have a user mapping for this connection, try to remove DB entry if no more connections exist for that user in the bar
                try
                {
                    if (ConnectionUserMap.TryRemove(connectionId, out var userId))
                    {
                        int otherConnectionsForUser = 0;
                        if (ActiveBarConnections.TryGetValue(barEntry.Key, out var bag))
                        {
                            otherConnectionsForUser = bag.Count(c => ConnectionUserMap.TryGetValue(c, out var uid) && uid == userId);
                        }

                        if (otherConnectionsForUser == 0)
                        {
                            if (Guid.TryParse(barEntry.Key, out var barGuid))
                            {
                                var removeResult = await _barUserEntryRepository.RemoveEntryAsync(barGuid, userId);
                                if (removeResult.IsSuccess)
                                    await _barUserEntryRepository.SaveChangesAsync();
                            }
                        }
                    }
                }
                catch
                {
                    // best-effort
                }

                // Notify remaining users in affected bars
                await Clients.Group(barEntry.Key).SendAsync("BarUsersUpdated", new { connectionId });
            }

            await base.OnDisconnectedAsync(exception);
        }

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
        public int GetBarConnectionCount(Guid barId)
        {
            string barIdStr = barId.ToString();
            if (ActiveBarConnections.TryGetValue(barIdStr, out var connections))
            {
                return connections.Count;
            }
            return 0;
        }


        public IEnumerable<Guid> GetActiveBars()
        {
            return ActiveBarConnections.Keys.Select(k => Guid.Parse(k));
        }
    }
}
