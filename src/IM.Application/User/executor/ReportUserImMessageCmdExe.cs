using System;
using System.Threading.Tasks;
using AutoMapper.Internal.Mappers;
using IM.Entities.Es;
using IM.Message;
using IM.User.Dtos;
using IM.User.Provider;
using Volo.Abp.Application.Services;

namespace IM.User.executor;

public class ReportUserImMessageCmdExe : ApplicationService
{
    private readonly IReportUserGateway _reportUserGateway;
    private readonly IUserProvider _userProvider;

    public ReportUserImMessageCmdExe(IReportUserGateway reportUserGateway, IUserProvider userProvider)
    {
        _reportUserGateway = reportUserGateway;
        _userProvider = userProvider;
    }
    
    public async Task ReportUserImMessage(ReportUserImMessageCmd reportUserImMessageCmd)
    {
        var user = await _userProvider.GetUserInfoAsync(Guid.Parse(reportUserImMessageCmd.UserId), reportUserImMessageCmd.UserAddress);
        var imUser = ObjectMapper.Map<UserIndex, ImUser>(user);
        var reportedUser = await _userProvider.GetUserInfoAsync(Guid.Parse(reportUserImMessageCmd.ReportedUserId),
            reportUserImMessageCmd.ReportedUserAddress);
        var imReportedUser = ObjectMapper.Map<UserIndex, ImUser>(reportedUser);
        var reportedMessage = ObjectMapper.Map<ReportUserImMessageCmd, ReportedMessage>(reportUserImMessageCmd); 
        await _reportUserGateway.ReportUser(imUser, imReportedUser, reportedMessage);
    }
}