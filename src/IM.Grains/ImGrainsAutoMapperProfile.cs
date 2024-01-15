using AutoMapper;
using IM.Dtos;
using IM.Feed;
using IM.Feed.Dtos;
using IM.Grains.Grain.Chat;
using IM.Grains.Grain.Group;
using IM.Grains.Grain.Mute;
using IM.Grains.Grain.User;
using IM.Grains.State.Chat;
using IM.Grains.State.Feed;
using IM.Grains.State.Group;
using IM.Grains.State.Mute;
using IM.Grains.State.User;
using IM.Message;
using IM.Message.Dtos;
using IM.Message.Etos;

namespace IM.Grains;

public class ImGrainsAutoMapperProfile : Profile
{
    public ImGrainsAutoMapperProfile()
    {
        //CreateMap<RegisterInfo, RegisterGrainDto>().ReverseMap();
        CreateMap<UserGrainDto, UserState>().ReverseMap();
        CreateMap<ChatMetaState, ChatMetaGrainDto>().ReverseMap();
        CreateMap<ChatMetaGrainDto, ChatMetaDto>().ReverseMap();
        CreateMap<EventMessageRequestDto, EventMessageEto>().ReverseMap();
        CreateMap<MessageInfoIndex, MessageInfoDto>().ReverseMap();
        CreateMap<FeedMetaState, FeedMetaDto>().ReverseMap();
        CreateMap<ListFeedResponseItemDto, FeedInfoGrainDto>().ReverseMap();
        CreateMap<ListFeedResponseItemDto, FeedInfoIndex>().ReverseMap();
        CreateMap<FeedInfoIndex, FeedInfoDto>().ReverseMap();
        CreateMap<FeedInfoIndex, ListFeedResponseItemDto>().ReverseMap();

        CreateMap<ListMessageResponseDto, MessageInfoIndex>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ChatId, opt => opt.MapFrom(src => src.ChannelUuid))
            .ForMember(dest => dest.FromId, opt => opt.MapFrom(src => src.From))
            .ForMember(dest => dest.Quote, opt => opt.MapFrom(src => src.Quote))
            .ForMember(dest => dest.CreateTimeInMs, opt => opt.MapFrom(src => src.CreateAt)).ReverseMap();
        
        CreateMap<GroupGrainDto, GroupState>().ReverseMap();
        CreateMap<MuteGrainDto, MuteState>().ReverseMap();
    }
}