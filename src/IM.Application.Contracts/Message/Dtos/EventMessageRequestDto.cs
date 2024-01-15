namespace IM.Message.Dtos;

public class EventMessageRequestDto
{
    public string ChannelUuid { get; set; }
    public string ToRelationId { get; set; }
    public string FromRelationId { get; set; }
    public int Action { get; set; }
}