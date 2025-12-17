using System;
using System.Collections.Generic;
using System.Linq;

namespace backend.Models
{
    public class Playlist
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<PlaylistSong> Songs { get; set; } = new();
        private PlaylistSong? _currentlyPlaying;

        public PlaylistSong AddSong(Song song, Guid addedBy)
        {
            var addedSong = new PlaylistSong
            {
                PlaylistId = Id,
                SongId = song.Id,
                Song = song,
                AddedByUserId = addedBy
            };
            Songs.Add(addedSong);
            return addedSong;
        }

        public Song? GetNextSong()
        {
            if (Songs.Count == 0) return null;

            // Ensure list is ordered
            ReorderByBids();
            // Determine next song based on highest bid first
            PlaylistSong nextSong;

            if (_currentlyPlaying == null)
            {
                // Pick song with highest bid first, fallback to lowest position
                nextSong = Songs
                    .OrderByDescending(s => s.CurrentBid)
                    .ThenBy(s => s.Position)
                    .First();
            }
            else
            {
                // Remove the current song from consideration temporarily
                var remainingSongs = Songs.Where(s => s != _currentlyPlaying).ToList();
                if (!remainingSongs.Any()) return null;

                nextSong = remainingSongs
                    .OrderByDescending(s => s.CurrentBid)
                    .ThenBy(s => s.Position)
                    .First();
            }

            _currentlyPlaying = nextSong;
            return _currentlyPlaying.Song;
        }

        public void RemoveSong(Guid songId)
        {
            var toRemove = Songs.FirstOrDefault(ps => ps.SongId == songId);
            if (toRemove != null)
            {
                // If the song being removed is currently playing, reset pointer
                if (_currentlyPlaying == toRemove)
                    _currentlyPlaying = null;

                Songs.Remove(toRemove);
            }
        }

        public void ReorderByBids()
        {
            Songs = Songs
                .OrderByDescending(s => s.CurrentBid)
                .ThenBy(s => s.AddedAt)
                .ToList();

            for (int i = 0; i < Songs.Count; i++)
            {
                Songs[i].Position = i + 1;
            }
        }
        public PlaylistSong? GetCurrentSong()
        {
            if (Songs.Count == 0) return null;
            ReorderByBids();
            return Songs.FirstOrDefault();
        }

        public void Clear()
        {
            Songs.Clear();
            _currentlyPlaying = null;
        }
    }
}