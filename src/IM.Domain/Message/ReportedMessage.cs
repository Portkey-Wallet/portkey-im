namespace IM.Message;

public class ReportedMessage
{
    public int ReportType {get; set; }
    public string Message {get; set; }
    public string MessageId {get; set; }
    public string Description {get; set; }
    public long ReportTime { get; set; }
    public string RelationId { get; set; }
    public string ChannelUuid { get; set; }
}