using backend.Models;
using backend.Common;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.Enums;
using backend.Utils.Errors;

namespace backend.Services
{
    public class SimplePlaylistService : IPlaylistService
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICreditService _creditService;

        public SimplePlaylistService(
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            ICreditService creditService)
        {
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
            _creditService = creditService;
        }

        public async Task<Result<PlaylistSong>> AddSongAsync(Guid userId, Song song)
        {
            var user = _userRepository.GetUserById(userId);
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
            var user = _userRepository.GetUserById(userId);
            if (user == null)
                return Result<Bid>.Failure("USER_NOT_FOUND", "User does not exist.");

            var playlistSong = await _playlistRepository.GetPlaylistSongBySongIdAsync(songId);
            if (playlistSong == null)
                return Result<Bid>.Failure("SONG_NOT_FOUND", "Song is not in the playlist.");

            if (amount <= 0)
                return Result<Bid>.Failure("INVALID_AMOUNT", "Bid amount must be positive.");

            var result_credit = _creditService.GetBalance(userId);
            if(result_credit.IsFailure)
                return Result<Bid>.Failure("INVALID_AMOUNT", "Bid amount must be positive.");
            
            int balance = result_credit.Value;
            if (balance < amount)
                return Result<Bid>.Failure(StandardErrors.NotFoundCredits);

            if (playlistSong.CurrentBidderId is Guid lastBidderId)
            {
                await _creditService.AddCredits(lastBidderId, playlistSong.CurrentBid, "Outbid refund", TransactionType.Refund);
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

            var result_transaction = await _creditService.SpendCredits(userId, amount, "Bidding on song", TransactionType.Spend);
            if(result_transaction.IsFailure)
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

        Task<Result<Boolean>> removePlaylistSongs (Guid playlistId)
        {

            throw new NotImplementedException();
            // return Result<Boolean>.Success(true);
            // var playlist =  _playlistRepository.GetByIdAsync(playlistId);
            // if (playlist == null)
            //     return Result<false>.Failure(StandardErrors.NotFoundPlaylist);
            // // UpdateAsync

            // return _playlistRepository.RemovePlaylistSongsAsync(playlistId);
        }

        Task<Result<bool>> IPlaylistService.removePlaylistSongs(Guid playlistId)
        {
            return removePlaylistSongs(playlistId);
        }
    }
}