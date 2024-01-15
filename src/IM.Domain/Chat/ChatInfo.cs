using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;

namespace IM.Chat;

public class ChatInfo : ChatBase, IIndexBuild
{
    public ChatType ChatType { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public string Owner { get; set; }
    public List<string> Members { get; set; }
    public string Description { get; set; }
    public bool IsDismiss { get; set; }
    public bool OpenAccess { get; set; }
    public string Announcement { get; set; }
    public bool PinAnnouncement { get; set; }
}