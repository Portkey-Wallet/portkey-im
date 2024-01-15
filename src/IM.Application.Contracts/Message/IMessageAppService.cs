using System.Threading.Tasks;
using IM.Dtos;
using IM.Message.Dtos;
using IM.Message.Etos;

namespace IM.Message;

public interface IMessageAppService : IBaseMessageAppService
{
    Task EventProcessAsync(EventMessageRequestDto input);

    Task ProcessMessageAsync(long startTime, long endTime, string startId, string endId, long pos,
        EventMessageEto eventData, ChatMetaDto dto);
}