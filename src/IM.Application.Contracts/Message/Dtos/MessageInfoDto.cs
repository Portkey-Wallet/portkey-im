namespace IM.Message.Dtos;

public class MessageInfoDto
{
    public string Id { get; set; }

    public MessageType MsgType { get; set; }

    public string ChatId { get; set; }

    public string FromId { get; set; }

    public string ToId { get; set; }

    public MessageInfoDto Quote { get; set; }

    public AtInfo AtList { get; set; }

    public long MsgPosition { get; set; }

    public long CreateTimeInMs { get; set; }
}