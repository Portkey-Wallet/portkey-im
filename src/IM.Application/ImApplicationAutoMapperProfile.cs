using System;
using AutoMapper;
using IM.ChannelContact.Dto;
using IM.ChannelContact.Etos;
using IM.Commons;
using IM.Contact.Dtos;
using IM.Entities.Es;
using IM.Feed.Etos;
using IM.Grains.Grain.Group;
using IM.Grains.Grain.Mute;
using IM.Grains.Grain.User;
using IM.Message;
using IM.Message.Dtos;
using IM.Message.Etos;
using IM.PinMessage;
using IM.PinMessage.Dtos;
using IM.User;
using IM.User.Dtos;
using IM.User.Etos;
using Volo.Abp.AutoMapper;
using CaHolderInfoDto = IM.Contact.Dtos.CaHolderInfoDto;

namespace IM;

public class ImApplicationAutoMapperProfile : Profile
{
    public ImApplicationAutoMapperProfile()
    {
        CreateMap<AddUserEto, UserIndex>();
        CreateMap<CaAddressInfo, CaAddressInfoDto>().ReverseMap();
        CreateMap<UserGrainDto, AddUserEto>();
        CreateMap<UserInfoDto, ImUserDto>().ReverseMap();
        CreateMap<AddressWithChain, ContactAddressDto>();
        CreateMap<ContactAddressDto, CaAddressInfoDto>();
        CreateMap<UserInfoDto, ContactProfileDto>()
            .ForMember(t => t.CreateTime, m => m.MapFrom(u => u.CreatedAt))
            .ForMember(t => t.Addresses, m => m.MapFrom(u => u.AddressWithChain))
            .ForMember(t => t.Name, m => m.MapFrom(u => u.CAName))
            .ForPath(t => t.ImInfo.RelationId, m => m.MapFrom(u => u.RelationId))
            .ForPath(t => t.ImInfo.PortkeyId, m => m.MapFrom(u => u.PortkeyId))
            .ForPath(t => t.ImInfo.Name,
                m => m.MapFrom(u => string.IsNullOrWhiteSpace(u.CAName) ? u.Name : string.Empty))
            .ReverseMap()
            ;
        CreateMap<ContactProfileDto, ContactListDto>()
            .ForPath(t => t.RelationId, m => m.MapFrom(u => u.ImInfo.RelationId))
            .ForPath(t => t.WalletName, m => m.MapFrom(u => u.CaHolderInfo.WalletName))
            ;

        CreateMap<UserIndex, ImUserDto>()
            .ForMember(t => t.PortkeyId, m => m.MapFrom(f => f.Id));
        CreateMap<UserIndex, ImUser>()
            .ForMember(t => t.PortkeyId, m => m.MapFrom(f => f.Id));
        CreateMap<ReportUserImMessageCmd, ReportedMessage>()
            .ForMember(t => t.ReportTime, m => m.MapFrom(f => DateTime.UtcNow.Ticks));
        CreateMap<CaHolderInfoDto, CaHolderDto>()
            .ForMember(t => t.UserId, m => m.MapFrom(f => f.UserId == Guid.Empty ? null : f.UserId.ToString()))
            ;
        CreateMap<ContactProfileDto, ContactInfoDto>()
            .ForMember(t => t.Id, m => m.MapFrom(f => f.Id == Guid.Empty ? null : f.Id.ToString()))
            .ForMember(t => t.UserId, m => m.MapFrom(f => f.UserId == Guid.Empty ? null : f.UserId.ToString()))
            .ReverseMap()
            ;
        CreateMap<AddressResultDto, ContactAddressDto>().ForMember(t => t.ChainName,
            m => m.MapFrom(f => f.ChainName.IsNullOrWhiteSpace() ? CommonConstant.DefaultChainName : f.ChainName));
        CreateMap<HolderInfoResultDto, ContactProfileDto>()
            .ForMember(t => t.Addresses, m => m.MapFrom(f => f.AddressInfos))
            .ReverseMap()
            ;

        CreateMap<GroupGrainDto, GroupAddOrUpdateEto>();
        CreateMap<GroupGrainDto, GroupDeleteEto>();
        CreateMap<MemberInfo, GroupMember>();
        CreateMap<ChannelDetailInfoResponseDto, GroupGrainDto>().ForMember(t => t.Id, m => m.MapFrom(f => f.Uuid))
            .Ignore(t => t.Members);
        CreateMap<SendMessageRequestDto, MessageSendEto>();
        CreateMap<GroupMember, GroupMemberInfo>();
        CreateMap<GroupAddOrUpdateEto, GroupIndex>();
        CreateMap<MuteEto, MuteIndex>();
        CreateMap<MuteGrainDto, MuteEto>();

        CreateMap<PinMessageQueryParamDto, PinMessageIndex>();
        CreateMap<CancelPinMessageParamDto, PinMessageIndex>();
        CreateMap<PinMessageQueryParamDto, ListPinMessageParam>();
        CreateMap<PinMessageParamDto, PinMessageIndex>();
        CreateMap<GuardianDto, CaAddressInfoDto>().ForMember(t => t.ChainId, m => m.MapFrom(f => f.ChainId))
            .ForMember(t => t.Address, m => m.MapFrom(f => f.CaAddress));
        CreateMap<PinMessageIndex, PinMessage.Dtos.PinMessage>()
            .ForMember(t => t.Quote, m => m.MapFrom(f => f.Quote));

        CreateMap<GuardianDto, ContactAddressDto>().ForMember(t => t.ChainId, m => m.MapFrom(f => f.ChainId))
            .ForMember(t => t.Address, m => m.MapFrom(f => f.CaAddress))
            .ForMember(t => t.ChainName,
                m => m.MapFrom(f => CommonConstant.DefaultChainName));

        CreateMap<MemberQueryDto, MemberInfo>();
    }
}