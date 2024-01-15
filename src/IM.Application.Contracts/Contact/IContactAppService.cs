using System.Threading.Tasks;
using IM.Contact.Dtos;
using Volo.Abp.Application.Dtos;

namespace IM.Contact;

public interface IContactAppService : IBaseContactAppService
{
    Task<ContactInfoDto> GetContactProfileAsync(ContactProfileRequestDto input);
    Task<PagedResultDto<ContactProfileDto>> GetListAsync(ContactGetListRequestDto input);
    Task<AddStrangerResultDto> AddStrangerAsync(AddStrangerDto input);
}