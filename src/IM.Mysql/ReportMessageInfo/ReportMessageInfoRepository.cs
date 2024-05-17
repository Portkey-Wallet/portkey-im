using Dapper;
using IM.Message;
using IM.Mysql.Template;
using IM.User;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace IM.Mysql.ReportMessageInfo;

public class ReportMessageInfoRepository : IReportUserGateway, ISingletonDependency
{
    private readonly IImTemplate _imTemplate;

    public ReportMessageInfoRepository(IImTemplate imTemplate)
    {
        _imTemplate = imTemplate;
    }
    
    public async Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage)
    {
        long currentTime = DateTime.UtcNow.Ticks;
        var parameters = new DynamicParameters();
        parameters.Add("@uid", user.PortkeyId);
        parameters.Add("@userAddressInfo", JsonConvert.SerializeObject(user.AddressWithChain));
        parameters.Add("@reportedUserId", reportedUser.PortkeyId);
        parameters.Add("@reportedUserAddressInfo", JsonConvert.SerializeObject(reportedUser.AddressWithChain));
        parameters.Add("@messageId", reportedMessage.MessageId);
        parameters.Add("@reportedType", reportedMessage.ReportType);
        parameters.Add("@reportedMessage", reportedMessage.Message);
        parameters.Add("@description", reportedMessage.Description);
        parameters.Add("@reportedTime", reportedMessage.ReportTime);
        parameters.Add("@createTime", currentTime);
        parameters.Add("@updateTime", currentTime);
        parameters.Add("@relationId", reportedMessage.RelationId);
        parameters.Add("@channelUuid", reportedMessage.ChannelUuid);
        var sql = "insert into report_message_info (uid, user_address_info, reported_user_id, reported_user_address_info, " +
                  "message_id, reported_type, reported_message, description, relation_id, channel_uuid, reported_time, create_time, update_time)" +
                  " values (@uid, @userAddressInfo, @reportedUserId, @reportedUserAddressInfo, @messageId, @reportedType, " +
                  "@reportedMessage, @description, @relationId, @channelUuid, @reportedTime, @createTime, @updateTime)";
        await _imTemplate.ExecuteAsync(sql, parameters);
    }
}