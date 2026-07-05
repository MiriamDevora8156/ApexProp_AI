using AutoMapper;
using ApexProp.Application.DTOs;
using ApexProp.Domain.Entities;

namespace ApexProp.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ============= Property Mappings =============
        CreateMap<Property, PropertyDto>()
            .ForMember(dest => dest.Images,
                opt => opt.MapFrom(src => src.Images.Select(img => img.Url).ToList()))
            .ForMember(dest => dest.NearbyLocations,
        opt => opt.MapFrom(src => src.NearbyLocations))
            .ReverseMap();

        CreateMap<CreatePropertyDto, Property>();

        // ============= User Mappings =============
        CreateMap<User, UserDto>()
            .ReverseMap();

        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));

        // ============= Location Mappings =============
        CreateMap<Location, LocationDto>()
            .ReverseMap();

        // ============= PriceHistory Mappings =============
        CreateMap<PriceHistory, PriceHistoryDto>()
            .ReverseMap();
    }
}