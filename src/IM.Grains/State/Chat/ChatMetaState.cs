using IM.Chat;

namespace IM.Grains.State.Chat;

public class ChatMetaState
{
    public string Id { get; set; }
    public ChatType ChatType { get; set; }

    public ProcessStatus ProcessStatus { get; set; }

    public DateTimeOffset LastProcessTimeInMs { get; set; }
    public long UpperTime { get; set; }
    public long LowerTime { get; set; }
    public string UpperId { get; set; }
    public string LowerId { get; set; }

    public long Pos { get; set; }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Id);
    }
}