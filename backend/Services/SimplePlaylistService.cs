using backend.Models;
using backend.Common;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class SimplePlaylistService : IPlaylistService
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly ISongRepository _songRepository;
        private readonly IBidRepository _bidRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICreditManager _creditManager;

        public SimplePlaylistService(
            IPlaylistRepository playlistRepository,
            ISongRepository songRepository,
            IBidRepository bidRepository,
            IUserRepository userRepository,
            ICreditManager creditManager)
        {
            _playlistRepository = playlistRepository;
            _songRepository = songRepository;
            _bidRepository = bidRepository;
            _userRepository = userRepository;
            _creditManager = creditManager;
        }

        public Result<PlaylistSong> AddSong(Guid userId, Song song)
        {
            var user = _userRepository.GetById(userId);
            if (user == null)
                return Result<PlaylistSong>.Failure("USER_NOT_FOUND", "User does not exist.");

            var existingSong = _songRepository.GetById(song.Id);
            if (existingSong == null)
            {
                _songRepository.Add(song);
            }

            var playlist = _playlistRepository.GetActivePlaylist();
            if (playlist == null)
                return Result<PlaylistSong>.Failure("PLAYLIST_NOT_FOUND", "No active playlist found.");

            var playlistSong = playlist.AddSong(song, userId);
            _playlistRepository.Update(playlist);

            return Result<PlaylistSong>.Success(playlistSong);
        }

        public Result<Bid> BidOnSong(Guid userId, Guid songId, int amount)
        {
            var user = _userRepository.GetById(userId);
            if (user == null)
                return Result<Bid>.Failure("USER_NOT_FOUND", "User does not exist.");

            var playlistSong = _playlistRepository.GetPlaylistSongBySongId(songId);
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
                _bidRepository.MarkLastBidAsRefunded(playlistSong.Id, lastBidderId);
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

            _playlistRepository.UpdatePlaylistSong(playlistSong);
            _bidRepository.Add(bid);

            _creditManager.SpendCredits(userId, amount, "Bidding on song");

            return Result<Bid>.Success(bid);
        }
        public Result<Song> GetNextSong(Guid playlistId)
        {
            var playlist = _playlistRepository.GetById(playlistId);
            if (playlist == null)
                return Result<Song>.Failure("PLAYLIST_NOT_FOUND", "Playlist does not exist.");

            var nextSong = playlist.GetNextSong();
            if (nextSong == null)
                return Result<Song>.Failure("NO_SONG_AVAILABLE", "No songs available in the playlist.");

            return Result<Song>.Success(nextSong);
        }
    }
}