using System.Threading.Tasks;
using IM.PinMessage.Dtos;

namespace IM.PinMessage;

public interface IPinMessageAppService
{
    Task<PinMessageResponse> ListPinMessageAsync(PinMessageQueryParamDto pinMessageQueryParamDto);
    Task<PinMessageResponseDto<bool>> PinMessageAsync(PinMessageParamDto paramDto);
    Task<UnpinMessageResponseDto<bool>> UnpinMessageAsync(CancelPinMessageParamDto paramDto);
    Task<UnpinMessageResponseDto<bool>> UnpinMessageAllAsync(CancelPinMessageAllParamDto paramDto);
}