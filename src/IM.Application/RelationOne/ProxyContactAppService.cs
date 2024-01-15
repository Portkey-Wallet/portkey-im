using System.Threading.Tasks;
using IM.Common;
using IM.RelationOne.Dtos.Contact;

namespace IM.RelationOne;

public class ProxyContactAppService : ImAppService, IProxyContactAppService
{
    
    private readonly IProxyRequestProvider _proxyRequestProvider;

    public ProxyContactAppService(IProxyRequestProvider proxyRequestProvider)
    {
        _proxyRequestProvider = proxyRequestProvider;
    }

    public async Task FollowAsync(FollowsRequestDto input)
    {
        await _proxyRequestProvider.PostAsync<object>("api/v1/follow", input);
    }

    public async Task UnFollowAsync(FollowsRequestDto input)
    {
        await _proxyRequestProvider.PostAsync<object>("api/v1/unfollow", input);
    }
    
    public async Task RemarkAsync(RemarkRequestDto input)
    {
        await _proxyRequestProvider.PostAsync<object>(ImUrlConstant.Remark, input);
    }
}