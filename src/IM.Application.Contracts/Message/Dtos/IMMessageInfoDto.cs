using System.Threading.Channels;

namespace IM.Message.Dtos;

public class IMMessageInfoDto
{
    private long Id { get; set; }
    private string SendUuid { get; set; }
    private string ChannelUuid { get; set; }
    private string From { get; set; }
    private string Type { get; set; }
    private long QuoteId { get; set; }
    private string MentionedUser { get; set; }
}