using System.Collections.Generic;
using IM.User.Dtos;
using IM.User.Etos;

namespace IM.Contact.Dtos;

public class ContactMergeDto
{
    public ImUserDto ImInfo { get; set; }
    public List<CaAddressInfoDto> Addresses { get; set; }
}