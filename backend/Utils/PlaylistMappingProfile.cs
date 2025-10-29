using AutoMapper;
using backend.Models;
using backend.Shared.DTOs;

namespace backend.Mapping
{
    public class PlaylistMappingProfile : Profile
    {
        public PlaylistMappingProfile()
        {
            // Map Song → SongDto
            CreateMap<Playlist, PlaylistDto>()
          .ForMember(dest => dest.BarId, opt => opt.Ignore())
          .ForMember(dest => dest.Songs, opt => opt.MapFrom(src => src.Songs)); // map PlaylistSong

            CreateMap<PlaylistSong, SongDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Song.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Song.Title))
                .ForMember(dest => dest.Artist, opt => opt.MapFrom(src => src.Song.Artist))
                .ForMember(dest => dest.Votes, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentBid, opt => opt.MapFrom(src => src.CurrentBid));
        }
    }
}