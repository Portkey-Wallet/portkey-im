using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace IM.Entities.Es;

public class GroupIndex : ImEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string Type { get; set; }
    public string Icon { get; set; }
    public List<GroupMemberInfo> Members { get; set; }
}

public class GroupMemberInfo
{
    [Keyword] public string RelationId { get; set; }
    [Keyword] public string PortKeyId { get; set; }
}