using Xunit;
using Moq;
using backend.Hubs;
using backend.Services.Background;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using backend.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using backend.Shared.Enums;
using System.Reflection;

namespace backend.Tests.Services.Background
{
    public class BarStateUpdaterServiceTests
    {
        [Fact]
        public async Task PlaylistUpdater_SendsSongStartAndEndNotifications()
        {
            var logger = new Mock<ILogger<BarStateUpdaterService>>();
            var hubCalls = new List<HubCall>();
            
            var hub = new Mock<IHubContext<BarHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            mockClientProxy
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((methodName, args, cancellationToken) =>
                {
                    hubCalls.Add(new HubCall(methodName, args));
                })
                .Returns(Task.CompletedTask);

            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            hub.Setup(h => h.Clients).Returns(mockClients.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            var scope = new Mock<IServiceScope>();
            var provider = new Mock<IServiceProvider>();
            scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            var barService = new Mock<IBarService>();
            var playlistRepo = new Mock<IPlaylistRepository>();
            provider.Setup(p => p.GetService(typeof(IBarService))).Returns(barService.Object);
            provider.Setup(p => p.GetService(typeof(IPlaylistRepository))).Returns(playlistRepo.Object);

            var barId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();
            var bar = new Bar { Id = barId, CurrentPlaylistId = playlistId };
            bar.SetState(BarState.Open);

            barService.Setup(s => s.GetActiveBars()).ReturnsAsync(new List<Bar> { bar });

            var playlist = new Playlist { Id = playlistId };
            var song = new Song
            {
                Id = Guid.NewGuid(),
                Title = "TestSong",
                Artist = "Artist",
                Duration = TimeSpan.FromMilliseconds(50)
            };

            AddSongToPlaylist(playlist, song);
            playlistRepo.Setup(r => r.GetByIdAsync(playlistId)).ReturnsAsync(playlist);

            var updateAsyncCalled = new ManualResetEventSlim(false);
            playlistRepo.Setup(r => r.UpdateAsync(It.IsAny<Playlist>()))
                        .Callback<Playlist>(pl => updateAsyncCalled.Set())
                        .Returns(Task.CompletedTask);

            var service = new BarStateUpdaterService(logger.Object, scopeFactory.Object, hub.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await service.StartAsync(cts.Token);
            var updateHappened = updateAsyncCalled.Wait(TimeSpan.FromSeconds(3));
            await service.StopAsync(cts.Token);

            Assert.True(updateHappened, "UpdateAsync was never called");
            barService.Verify(s => s.GetActiveBars(), Times.AtLeastOnce);
            playlistRepo.Verify(r => r.GetByIdAsync(playlistId), Times.AtLeastOnce);
            playlistRepo.Verify(r => r.UpdateAsync(It.IsAny<Playlist>()), Times.AtLeastOnce);
            
            Assert.True(hubCalls.Count >= 2, $"Expected 2+ hub calls, got {hubCalls.Count}");
            
            var playlistUpdatedCalls = hubCalls.FindAll(call => call.MethodName == "PlaylistUpdated");
            Assert.True(playlistUpdatedCalls.Count >= 2, $"Expected 2+ PlaylistUpdated calls, got {playlistUpdatedCalls.Count}");

            var foundSongStarted = false;
            var foundSongEnded = false;

            foreach (var call in playlistUpdatedCalls)
            {
                if (call.Arguments.Length >= 2)
                {
                    var firstArg = call.Arguments[0];
                    if (firstArg?.GetType().Name == "PlaylistEvent")
                    {
                        var action = firstArg.GetType().GetProperty("Action")?.GetValue(firstArg) as string;
                        if (action == "song_started") foundSongStarted = true;
                        if (action == "song_ended") foundSongEnded = true;
                    }
                }
            }

            Assert.True(foundSongStarted, "Missing song_started notification");
            Assert.True(foundSongEnded, "Missing song_ended notification");
        }

        private void AddSongToPlaylist(Playlist playlist, Song song)
        {
            var addSongMethod = typeof(Playlist).GetMethod("AddSong", BindingFlags.Public | BindingFlags.Instance);
            if (addSongMethod != null)
            {
                var parameters = addSongMethod.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Song))
                {
                    addSongMethod.Invoke(playlist, new object[] { song, Guid.NewGuid() });
                    return;
                }
            }
            throw new InvalidOperationException("Could not add song to playlist");
        }

        private class HubCall
        {
            public string MethodName { get; }
            public object[] Arguments { get; }

            public HubCall(string methodName, object[] arguments)
            {
                MethodName = methodName;
                Arguments = arguments;
            }
        }
    }
}