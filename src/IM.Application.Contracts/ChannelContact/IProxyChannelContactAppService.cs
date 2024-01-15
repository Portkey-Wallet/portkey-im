using System.Collections.Generic;
using System.Threading.Tasks;
using IM.ChannelContact.Dto;

namespace IM.ChannelContact;

public interface IProxyChannelContactAppService : IBaseChannelContactAppService
{
    Task BuildUserNameAsync(List<MemberInfo> memberInfos, string caToken = null);
}