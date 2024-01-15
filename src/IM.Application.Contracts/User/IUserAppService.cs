using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Contact.Dtos;
using IM.User.Dtos;

namespace IM.User;

public interface IUserAppService : IBaseUserAppService
{
    Task<ImUserDto> GetImUserInfoAsync(string relationId);
    Task<ImUserDto> GetImUserAsync(string address);
    Task<ContactProfileDto> GetContactAsync(Guid contactUserId);
}