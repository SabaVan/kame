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
        private readonly IBarPlaylistEntryRepository _barPlaylistEntries;

        public PlaylistService(
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            ICreditService creditService,
            IBarPlaylistEntryRepository barPlaylistEntries)
        {
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
            _creditService = creditService;
            _barPlaylistEntries = barPlaylistEntries;
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
                AddedAt = DateTime.UtcNow,
                Position = playlist.Songs.Count + 1  // Set position: last in queue
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

            // capture previous bidder info to refund later
            Guid? previousBidderId = playlistSong.CurrentBidderId;
            int previousBidAmount = playlistSong.CurrentBid;

            var addBidResult = playlistSong.AddBid(amount);
            if (!addBidResult.IsSuccess)
                return Result<Bid>.Failure("BID_ERROR", addBidResult.Error?.Message ?? "Failed to add bid.");

            // resolve bar for the playlist and supply it when spending credits for the bid
            var bars = await _barPlaylistEntries.GetBarsForPlaylistAsync(playlistSong.PlaylistId);
            var theBar = bars?.FirstOrDefault();
            Guid? theBarId = theBar?.Id;

            // attempt to spend credits for the new bidder first
            var result_transaction = await _creditService.SpendCredits(userId, amount, "Bidding on song", TransactionType.Spend, theBarId);
            if (result_transaction.IsFailure)
            {
                // restore in-memory bid state
                playlistSong.CurrentBid = previousBidAmount;
                return Result<Bid>.Failure(StandardErrors.TransactionErrorSpend);
            }

            // set the current bidder now that spend succeeded
            playlistSong.CurrentBidderId = userId;

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

            // try to refund previous bidder (best-effort). If refund fails, do not fail the bid; log if possible.
            if (previousBidderId.HasValue && previousBidAmount > 0)
            {
                try
                {
                    var barsForPlaylist = await _barPlaylistEntries.GetBarsForPlaylistAsync(playlistSong.PlaylistId);
                    var playlistBar = barsForPlaylist?.FirstOrDefault();
                    Guid? barId = playlistBar?.Id;

                    await _creditService.AddCredits(previousBidderId.Value, previousBidAmount, "Outbid refund", TransactionType.Refund, barId);
                }
                catch
                {
                    // swallow refund errors to avoid failing the bid; could add logging here
                }
            }

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

        public async Task<Result<PlaylistSong>> GetCurrentSongAsync(Guid playlistId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null)
                return Result<PlaylistSong>.Failure("PLAYLIST_NOT_FOUND", "Playlist does not exist.");

            var currentSong = playlist.GetCurrentSong(); // The new method
            if (currentSong == null)
                return Result<PlaylistSong>.Failure("NO_SONG_AVAILABLE", "No songs available in the playlist.");

            return Result<PlaylistSong>.Success(currentSong);
        }
        public async Task<Result<Playlist>> ReorderAndSavePlaylistAsync(Guid playlistId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null)
                return Result<Playlist>.Failure("PLAYLIST_NOT_FOUND", "Playlist not found.");

            // Reorder in-memory
            playlist.ReorderByBids();

            // Persist each song's new position
            foreach (var song in playlist.Songs)
            {
                await _playlistRepository.UpdatePlaylistSongAsync(song);
            }

            return Result<Playlist>.Success(playlist);
        }
    }
}