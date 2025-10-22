using AutoMapper;
using backend.Models;
using backend.Shared.DTOs;
using backend.Shared.Enums;

public class BarMappingProfile : Profile
{
    public BarMappingProfile()
    {
        CreateMap<Bar, BarDto>()
    .ForMember(dest => dest.State, opt => opt.MapFrom(src => MapState(src.State)))
    .ForMember(dest => dest.OpenAtUtc, opt => opt.MapFrom(src => src.OpenAtUtc))
    .ForMember(dest => dest.CloseAtUtc, opt => opt.MapFrom(src => src.CloseAtUtc))
    .ForMember(dest => dest.CurrentPlaylist, opt => opt.MapFrom(src => string.Empty));

    }

    private static string MapState(BarState state)
    {
        return state switch
        {
            BarState.Closed => "Closed",
            BarState.Open => "Open",
            BarState.Maintenance => "Maintenance",
            BarState.Paused => "Paused",
            _ => "Unknown"
        };
    }
}
