namespace backend.Models
{
    public class Playlist
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<PlaylistSong> Songs { get; set; } = [];
        private PlaylistSong? _currentlyPlaying;

        public PlaylistSong AddSong(Song song, Guid addedBy)
        {
            PlaylistSong addedSong = new()
            { PlaylistId = Id, SongId = song.Id, Song = song, AddedByUserId = addedBy };
            Songs.Add(addedSong);

            return addedSong;
        }

        public Song? GetNextSong()
        {
            if (Songs.Count == 0) return null;

            if (_currentlyPlaying == null)
            {
                _currentlyPlaying = Songs[0];
                return _currentlyPlaying.Song;
            }

            int index = Songs.IndexOf(_currentlyPlaying);

            if (index == -1) return null;

            // Loop back to start if at the end of the list
            if (index == Songs.Count - 1)
                _currentlyPlaying = Songs[0];
            else
                _currentlyPlaying = Songs[index + 1];

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
            Songs.Sort();
            for (int i = 0; i < Songs.Count; i++)
            {
                Songs[i].Position = i + 1;
            }
        }

        public void Clear()
        {
            Songs.Clear();
            _currentlyPlaying = null;
        }
    }
}