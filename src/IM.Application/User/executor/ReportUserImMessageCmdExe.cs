using System;
using System.Threading.Tasks;
using AutoMapper.Internal.Mappers;
using IM.Entities.Es;
using IM.Message;
using IM.User.Dtos;
using IM.User.Provider;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace IM.User.executor;

public class ReportUserImMessageCmdExe : ApplicationService
{
    private readonly ILogger<ReportUserImMessageCmdExe> _logger;
    private readonly IUserProvider _userProvider;

    public ReportUserImMessageCmdExe(ILogger<ReportUserImMessageCmdExe> logger, IUserProvider userProvider)
    {
        _logger = logger;
        _userProvider = userProvider;
    }
    
    public async Task ReportUserImMessage(ReportUserImMessageCmd reportUserImMessageCmd)
    {
        Guid userId, reportedUserId;
        try
        {
            userId = Guid.Parse(reportUserImMessageCmd.UserId);
            reportedUserId = Guid.Parse(reportUserImMessageCmd.ReportedUserId);
        }
        catch (Exception e)
        {
            _logger.LogError("the input user id is:{0}, reported user id is:{1}", reportUserImMessageCmd.UserId, reportUserImMessageCmd.ReportedUserId);
            throw new UserFriendlyException("user/reportedUser id format is error, Guid should contain 32 digits with 4 dashes (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)");
        }
        var user = await _userProvider.GetUserInfoByIdAsync(userId);
        if (user is null)
        {
            throw new UserFriendlyException("user does not exist");
        }
        var imUser = ObjectMapper.Map<UserIndex, ImUser>(user);
        var reportedUser = await _userProvider.GetUserInfoByIdAsync(reportedUserId);
        if (reportedUser is null)
        {
            throw new UserFriendlyException("reported user does not exist");
        }
        var imReportedUser = ObjectMapper.Map<UserIndex, ImUser>(reportedUser);
        var reportedMessage = ObjectMapper.Map<ReportUserImMessageCmd, ReportedMessage>(reportUserImMessageCmd); 
        await _userProvider.ReportUser(imUser, imReportedUser, reportedMessage);
    }
}