using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Repositories;
using backend.Utils.Errors;
using backend.Services.Interfaces;
using backend.Utils;
using backend.Enums;

namespace backend.Services
{
    public class SimpleBarService : IBarService
    {
        private readonly IBarRepository _bars;
        // private readonly IUserRepository _users;
        // private readonly IPlaylistRepository _playlistService;
        // private readonly ICreditManager _credits;
        public SimpleBarService(IBarRepository bars)
        {
            ArgumentNullException.ThrowIfNull(bars);
            //ArgumentNullException.ThrowIfNull(users);
            //ArgumentNullException.ThrowIfNull(playlistService);
            //ArgumentNullException.ThrowIfNull(credits);

            _bars = bars;
            //_users = users;
            //_playlistService = playlistService;
            //_credits = credits;
        }
        public async Task<Bar?> GetDefaultBar()
        {
            return (await _bars.GetAllAsync()).FirstOrDefault();
        }
        public async Task<Result<Bar?>> SetBarState(Guid BarId, BarState newState)
        {
            Bar? bar = await _bars.GetByIdAsync(BarId);
            if (bar == null)
                return Result<Bar?>.Failure(StandardErrors.NotFound);
            bar.SetState(newState);
            await _bars.SaveChangesAsync();
            return Result<Bar?>.Success(bar);
        }
    }
}
