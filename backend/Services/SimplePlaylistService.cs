using backend.Models;
using backend.Common;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class SimplePlaylistService : IPlaylistService
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICreditManager _creditManager;

        public SimplePlaylistService(
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            ICreditManager creditManager)
        {
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
            _creditManager = creditManager;
        }

        public async Task<Result<PlaylistSong>> AddSongAsync(Guid userId, Song song)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result<PlaylistSong>.Failure("USER_NOT_FOUND", "User does not exist.");

            var playlist = await _playlistRepository.GetActivePlaylistAsync();
            if (playlist == null)
                return Result<PlaylistSong>.Failure("PLAYLIST_NOT_FOUND", "No active playlist found.");

            var playlistSong = playlist.AddSong(song, userId);

            await _playlistRepository.UpdateAsync(playlist);

            return Result<PlaylistSong>.Success(playlistSong);
        }

        public async Task<Result<Bid>> BidOnSongAsync(Guid userId, Guid songId, int amount)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result<Bid>.Failure("USER_NOT_FOUND", "User does not exist.");

            var playlistSong = await _playlistRepository.GetPlaylistSongBySongIdAsync(songId);
            if (playlistSong == null)
                return Result<Bid>.Failure("SONG_NOT_FOUND", "Song is not in the playlist.");

            if (amount <= 0)
                return Result<Bid>.Failure("INVALID_AMOUNT", "Bid amount must be positive.");

            var balance = _creditManager.GetBalance(userId);
            if (balance < amount)
                return Result<Bid>.Failure("INSUFFICIENT_CREDITS", "Not enough credits to place bid.");

            if (playlistSong.CurrentBidderId is Guid lastBidderId)
            {
                _creditManager.RefundCredits(lastBidderId, playlistSong.CurrentBid, "Outbid refund");
            }

            var addBidResult = playlistSong.AddBid(amount);
            if (!addBidResult.IsSuccess)
                return Result<Bid>.Failure("BID_ERROR", addBidResult.Error?.Message ?? "Failed to add bid.");

            var bid = new Bid
            {
                UserId = userId,
                PlaylistSongId = playlistSong.Id,
                Amount = amount,
                IsRefunded = false
            };

            await _playlistRepository.UpdatePlaylistSongAsync(playlistSong);

            _creditManager.SpendCredits(userId, amount, "Bidding on song");

            return Result<Bid>.Success(bid);
        }

        public async Task<Result<Song>> GetNextSongAsync(Guid playlistId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null)
                return Result<Song>.Failure("PLAYLIST_NOT_FOUND", "Playlist does not exist.");

            var nextSong = playlist.GetNextSong();
            if (nextSong == null)
                return Result<Song>.Failure("NO_SONG_AVAILABLE", "No songs available in the playlist.");

            return Result<Song>.Success(nextSong);
        }

        public async Task<Result<Playlist>> ReorderAndSavePlaylistAsync(Guid playlistId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null)
                return Result<Playlist>.Failure("PLAYLIST_NOT_FOUND", "Playlist not found.");

            playlist.ReorderByBids();

            await _playlistRepository.UpdateAsync(playlist);

            return Result<Playlist>.Success(playlist);
        }
    }
}