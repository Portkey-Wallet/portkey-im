using System.Threading.Tasks;
using IM.RelationOne.Dtos.Contact;

namespace IM.Contact;

public interface IBaseContactAppService
{
    Task FollowAsync(FollowsRequestDto input);
    Task UnFollowAsync(FollowsRequestDto input);
    Task RemarkAsync(RemarkRequestDto input);
}