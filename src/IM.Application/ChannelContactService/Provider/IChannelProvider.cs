using System.Threading.Tasks;
using IM.ChannelContact.Dto;
using Volo.Abp.DependencyInjection;

namespace IM.ChannelContactService.Provider;

public interface IChannelProvider
{
    Task<MemberInfo> GetMemberInfoAsync(string channelId, string relationId);
}

public class ChannelProvider : IChannelProvider, ISingletonDependency
{
    public Task<MemberInfo> GetMemberInfoAsync(string channelId, string relationId)
    {
        throw new System.NotImplementedException();
    }
}