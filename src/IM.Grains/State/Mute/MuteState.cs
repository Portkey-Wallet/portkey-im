namespace IM.Grains.State.Mute;

public class MuteState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public string GroupId { get; set; }
    public bool Mute { get; set; }
    public DateTime LastModificationTime { get; set; }
}