using System;

namespace IM.User.Dtos;

public class BlockUserInfoDto
{
    public int Id { get; set; }
    public string RelationId { get; set; }
    public string BlockRelationId { get; set; }

    public int IsEffective { get; set; }
    public DateTime CreateTime  { get; set; }
    public DateTime UpdateTime { get; set; }

}