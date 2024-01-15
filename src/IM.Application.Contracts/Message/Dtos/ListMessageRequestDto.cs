namespace IM.Message.Dtos;

public class ListMessageRequestDto
{
    public string ChannelUuid { get; set; }
    public string ToRelationId { get; set; }
    public long MaxCreateAt { get; set; }
    public int Limit { get; set; }
}