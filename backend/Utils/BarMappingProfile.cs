using AutoMapper;
using backend.Models;
using backend.DTOs;
using backend.Enums;

public class BarMappingProfile : Profile
{
    public BarMappingProfile()
    {
        CreateMap<Bar, BarDto>()
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => MapState(src.State)))
          .ForMember(dest => dest.OpenAt, opt => opt.MapFrom(src => src.OpenAtUtc.ToString("HH:mm")))
          .ForMember(dest => dest.CloseAt, opt => opt.MapFrom(src => src.CloseAtUtc.ToString("HH:mm")));
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
