using System;
using backend.Models;
using backend.Common;
using Xunit;

namespace backend.Tests.Models
{
    public class PlaylistSongTests
    {
        [Fact]
        public void AddBid_PositiveHigherThanCurrent_Succeeds()
        {
            var song = new Song { Title = "Test", Artist = "Artist" };
            var ps = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 10
            };

            var result = ps.AddBid(20);

            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value);
            Assert.Equal(20, ps.CurrentBid);
        }

        [Fact]
        public void AddBid_NonPositiveAmount_Fails()
        {
            var song = new Song { Title = "Test", Artist = "Artist" };
            var ps = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 10
            };

            var result = ps.AddBid(0);

            Assert.False(result.IsSuccess);
            Assert.Equal("INVALID_AMOUNT", result?.Error?.Code);
        }

        [Fact]
        public void AddBid_LowerThanCurrent_Fails()
        {
            var song = new Song { Title = "Test", Artist = "Artist" };
            var ps = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 50
            };

            var result = ps.AddBid(40);

            Assert.False(result.IsSuccess);
            Assert.Equal("LOWER_THAN_CURRENT", result?.Error?.Code);
            Assert.Equal(50, ps.CurrentBid); // CurrentBid should not change
        }

        [Fact]
        public void CompareTo_HigherBidComesFirst()
        {
            var song1 = new Song { Title = "S1", Artist = "A" };
            var song2 = new Song { Title = "S2", Artist = "B" };

            var ps1 = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song1,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 100
            };

            var ps2 = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song2,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 50
            };

            Assert.True(ps1.CompareTo(ps2) < 0); // ps1 has higher bid, comes first
            Assert.True(ps2.CompareTo(ps1) > 0);
        }

        [Fact]
        public void CompareTo_SameBid_EarlierAddedComesFirst()
        {
            var song1 = new Song { Title = "S1", Artist = "A" };
            var song2 = new Song { Title = "S2", Artist = "B" };

            var now = DateTime.UtcNow;
            var ps1 = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song1,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 100,
                AddedAt = now
            };

            var ps2 = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = song2,
                AddedByUserId = Guid.NewGuid(),
                CurrentBid = 100,
                AddedAt = now.AddMinutes(1)
            };

            Assert.True(ps1.CompareTo(ps2) < 0); // ps1 added earlier
            Assert.True(ps2.CompareTo(ps1) > 0);
        }

        [Fact]
        public void CompareTo_NullOther_ReturnsOne()
        {
            var ps = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = Guid.NewGuid(),
                Song = new Song { Title = "S", Artist = "A" },
                AddedByUserId = Guid.NewGuid()
            };

            Assert.Equal(1, ps.CompareTo(null));
        }
    }
}