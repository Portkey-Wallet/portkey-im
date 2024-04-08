using System.Threading.Channels;

namespace IM.Message.Dtos;

public class IMMessageInfoDto
{
    public long Id { get; set; }
    public string SendUuid { get; set; }
    public string ChannelUuid { get; set; }
    public string From { get; set; }
    public string Type { get; set; }
    public long QuoteId { get; set; }
    public string MentionedUser { get; set; }
    public int Status { get; set; }
}