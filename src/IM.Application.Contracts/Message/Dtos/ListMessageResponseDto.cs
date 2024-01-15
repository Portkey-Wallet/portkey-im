using IM.Commons;
using IM.RedPackage;
using JetBrains.Annotations;

namespace IM.Message.Dtos;

public class ListMessageResponseDto
{
    public string Id { get; set; }
    public string SendUuid { get; set; }
    public string ChannelUuid { get; set; }
    public long CreateAt { get; set; }
    public string Type { get; set; }
    public string From { get; set; }
    public string FromName { get; set; }
    public string FromAvatar { get; set; }
    public string Content { get; set; }
    public RedPackageMessage RedPackage { get; set; } = new();
    [CanBeNull] public ListMessageResponseDto Quote { get; set; }
}