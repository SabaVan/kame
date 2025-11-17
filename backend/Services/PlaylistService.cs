using backend.Models;
using backend.Common;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.Enums;
using backend.Utils.Errors;

namespace backend.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICreditService _creditService;

        public PlaylistService(
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            ICreditService creditService)
        {
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
            _creditService = creditService;
        }

        public async Task<Result<PlaylistSong>> AddSongAsync(Guid userId, Guid playlistId, Song song)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null)
                return Result<PlaylistSong>.Failure("USER_NOT_FOUND", "User does not exist.");

            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null)
                return Result<PlaylistSong>.Failure("PLAYLIST_NOT_FOUND", "Playlist does not exist.");

            // Add song to Songs table if it doesn't exist
            var existingSong = await _playlistRepository.GetSongByIdAsync(song.Id);
            if (existingSong == null)
            {
                await _playlistRepository.AddSongAsync(song);
            }

            // Prevent duplicates
            if (playlist.Songs.Any(ps => ps.SongId == song.Id))
            {
                return Result<PlaylistSong>.Failure("DUPLICATE_SONG", "This song is already in the playlist.");
            }

            // Create PlaylistSong
            var playlistSong = new PlaylistSong
            {
                PlaylistId = playlist.Id,
                SongId = song.Id,
                Song = song,
                AddedByUserId = userId,
                AddedAt = DateTime.UtcNow
            };

            await _playlistRepository.AddPlaylistSongAsync(playlistSong);

            return Result<PlaylistSong>.Success(playlistSong);
        }
        public async Task<Result<Bid>> BidOnSongAsync(Guid userId, Guid songId, int amount)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null)
                return Result<Bid>.Failure("USER_NOT_FOUND", "User does not exist.");

            var playlistSong = await _playlistRepository.GetPlaylistSongBySongIdAsync(songId);
            if (playlistSong == null)
                return Result<Bid>.Failure("SONG_NOT_FOUND", "Song is not in the playlist.");

            object amountBoxed = amount; // Boxing
            int amountUnboxed = (int)amountBoxed; // Unboxing

            if (amountUnboxed <= 0)
                return Result<Bid>.Failure("INVALID_AMOUNT", "Bid amount must be positive.");

            var result_credit = _creditService.GetBalance(userId);
            if (result_credit.IsFailure)
            {
                return Result<Bid>.Failure(StandardErrors.NotFound);
            }


            int balance = result_credit.Value;
            if (balance < amount)
            {
                return Result<Bid>.Failure("NOT_ENOUGH_CREDITS", "User does not have enough credits.");
            }

            if (playlistSong.CurrentBidderId is Guid lastBidderId)
            {
                await _creditService.AddCredits(lastBidderId, playlistSong.CurrentBid, "Outbid refund", TransactionType.Refund);
            }

            var addBidResult = playlistSong.AddBid(amount);
            if (!addBidResult.IsSuccess)
                return Result<Bid>.Failure("BID_ERROR", addBidResult.Error?.Message ?? "Failed to add bid.");


            var playlist = await _playlistRepository.GetByIdAsync(playlistSong.PlaylistId);
            if (playlist != null)
            {
                playlist.ReorderByBids();
                await _playlistRepository.UpdateAsync(playlist);
            }

            var bid = new Bid
            {
                UserId = userId,
                PlaylistSongId = playlistSong.Id,
                Amount = amount,
                IsRefunded = false
            };

            await _playlistRepository.UpdatePlaylistSongAsync(playlistSong);

            var result_transaction = await _creditService.SpendCredits(userId, amount, "Bidding on song", TransactionType.Spend);
            if (result_transaction.IsFailure)
                return Result<Bid>.Failure(StandardErrors.TransactionErrorSpend);

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

        public async Task<Result<Playlist>> GetByIdAsync(Guid playlistId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null)
                return Result<Playlist>.Failure("PLAYLIST_NOT_FOUND", "Playlist does not exist.");
            return Result<Playlist>.Success(playlist);
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