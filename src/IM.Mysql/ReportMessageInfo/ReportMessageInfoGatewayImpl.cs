using Dapper;
using IM.Message;
using IM.Mysql.Template;
using IM.User;

namespace IM.Mysql.ReportMessageInfo;

public class ReportMessageInfoGatewayImpl : IReportUserGateway
{
    private readonly IImTemplate _imTemplate;

    public ReportMessageInfoGatewayImpl(IImTemplate imTemplate)
    {
        _imTemplate = imTemplate;
    }
    
    public async Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage)
    {
        long currentTime = DateTime.UtcNow.Ticks;
        var parameters = new DynamicParameters();
        parameters.Add("@uid", user.PortkeyId);
        parameters.Add("@userAddress", user.AddressWithChain.First().Address);
        parameters.Add("@reportedUserId", reportedUser.PortkeyId);
        parameters.Add("@reportedUserAddress", reportedUser.AddressWithChain.First().Address);
        parameters.Add("@messageId", reportedMessage.MessageId);
        parameters.Add("@reportedType", reportedMessage.ReportType);
        parameters.Add("@reportedMessage", reportedMessage.Message);
        parameters.Add("@description", reportedMessage.Description);
        parameters.Add("@reportedTime", reportedMessage.ReportTime);
        parameters.Add("@createTime", currentTime);
        parameters.Add("@updateTime", currentTime);
        var sql = "insert into report_message_info (uid, user_address, reported_user_id, reported_user_address, " +
                  "message_id, reported_type, reported_message, description, reported_time, create_time, update_time)" +
                  " values (@uid, @userAddress, @reportedUserId, @reportedUserAddress, @messageId, @reportedType, " +
                  "@reportedMessage, @description, @reportedTime, @createTime, @updateTime)";
        await _imTemplate.ExecuteAsync(sql, parameters);
    }
}