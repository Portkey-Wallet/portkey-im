using System.Threading.Tasks;
using IM.Transfer.Dtos;

namespace IM.Transfer;

public interface ITransferAppService
{
    Task<TransferOutputDto> SendTransferAsync(TransferInputDto input);
    Task<TransferResultDto> GetResultAsync(string transferId);
}