using System;
using System.Collections.Generic;

namespace IM.User.Dtos;

public class UserInfoRequestDto
{
    public string Address { get; set; }
    public List<string> Fields { get; set; }
}