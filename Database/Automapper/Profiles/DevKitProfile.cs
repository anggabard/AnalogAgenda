using AutoMapper;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;

namespace Database.Automapper.Profiles;

public class DevKitProfile : Profile
{
    public DevKitProfile()
    {
        CreateMap<DevKitEntity, DevKitDto>()
            .ForMember(dest => dest.PurchasedOn,
                opt => opt.MapFrom(src => DateOnly.FromDateTime(src.PurchasedOn)))
            .ForMember(dest => dest.MixedOn,
                opt => opt.MapFrom(src => DateOnly.FromDateTime(src.MixedOn)));

        CreateMap<DevKitDto, DevKitEntity>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src => src.Type.ToEnum<EDevKitType>()))
            .ForMember(dest => dest.PurchasedBy,
                opt => opt.MapFrom(src => src.PurchasedBy.ToEnum<EUsernameType>()))
            .ForMember(dest => dest.PurchasedOn,
                opt => opt.MapFrom(src => new DateTime(src.PurchasedOn, TimeOnly.MinValue, DateTimeKind.Utc)))
            .ForMember(dest => dest.MixedOn,
                opt => opt.MapFrom(src => new DateTime(src.MixedOn, TimeOnly.MinValue, DateTimeKind.Utc)));
    }
}
