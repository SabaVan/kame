using System.Text.Json;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories
{
  public class ExternalAPISongRepository : ISongRepository
  {
    private readonly HttpClient _httpClient;
    private readonly string _clientId;

    public ExternalAPISongRepository(HttpClient httpClient)
    {
      _httpClient = httpClient;

      // Read from environment variables
      _clientId = Environment.GetEnvironmentVariable("JAMENDO_CLIENT_ID")
                  ?? throw new InvalidOperationException("JAMENDO_CLIENT_ID is not set in environment variables.");
    }

    public async Task<IEnumerable<Song>> SearchAsync(string query, int limit = 10)
    {
      if (string.IsNullOrWhiteSpace(query))
        return Enumerable.Empty<Song>();

      string url = $"https://api.jamendo.com/v3.0/tracks/?" +
                   $"client_id={_clientId}" +
                   $"&format=json" +
                   $"&limit={limit}" +
                   $"&search={Uri.EscapeDataString(query)}";

      var response = await _httpClient.GetStringAsync(url);
      var json = JsonDocument.Parse(response);

      return json.RootElement
          .GetProperty("results")
          .EnumerateArray()
          .Select(track => new Song
          {
            Id = Guid.NewGuid(),
            Title = track.GetProperty("name").GetString() ?? "Unknown",
            Artist = track.GetProperty("artist_name").GetString() ?? "Unknown",
            Album = track.GetProperty("album_name").GetString() ?? "",
            StreamUrl = track.GetProperty("audio").GetString() ?? ""
          });
    }
  }
}