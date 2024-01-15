using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace IM.Entities.Es;

public class MuteIndex : ImEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string GroupId { get; set; }
    public bool Mute { get; set; }
    public DateTime LastModificationTime { get; set; }
}