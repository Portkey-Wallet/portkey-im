namespace IM.Grains.State.MessageState;

public class MessageState
{
    public string Id { get; set; }
    public string ContextEncrypt { get; set; }
    public int KeyVersion { get; set; }
}