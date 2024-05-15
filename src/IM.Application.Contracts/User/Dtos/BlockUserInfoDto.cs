using System;

namespace IM.User.Dtos;

public class BlockUserInfoDto
{
    public int Id { get; set; }
    public string UId { get; set; }
    public string BlockUId { get; set; }
    public DateTime CreateTime  { get; set; }
    public DateTime UpdateTime { get; set; }

}